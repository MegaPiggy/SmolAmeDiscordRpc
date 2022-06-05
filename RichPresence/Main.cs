using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SALT;
using SALT.Extensions;
using SALT.Registries;
using SALT.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RichPresence
{
	public class Main : ModEntryPoint
	{
		public const string CLIENT_ID = "857294083206938644";
		public static readonly DateTime EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		public const float RPC_UPDATE_TIME = 15; // RPC dll should handle rate limiting just fine.

		public static Assembly execAssembly;
		public const Level DONT_DESTROY_ON_LOAD = (Level)(-1);
		public static Level Level => Levels.CurrentLevel;
		public static Character Character
		{
			get
			{
				var player = SALT.Main.actualPlayer;
				if (player != null)
				{
					var characterPack = player.GetCurrentCharacterPack();
					if (characterPack != null)
					{
						if (characterPack.HasComponent<CharacterIdentifiable>())
						{
							return characterPack.GetComponent<CharacterIdentifiable>().Id;
						}
					}
				}
				return Character.NONE;
			}
		}
		public static TimeSpan? LevelTime
		{
			get
			{
				var main = SALT.Main.mainScript;
				if (main != null)
					return TimeSpan.FromSeconds(main.levelTime);
				return null;
			}
		}
		public static LevelManager LevelManager => LevelManager.levelManager;
		public static int MoustacheCount { get => Main.LevelManager.collectedMoustaches; }
		public static int MoustacheQuota { get => Main.LevelManager.moustacheQuotaInt; }
		public static int BubbaTokens { get => Main.LevelManager.bubbaTokens.Where(torf => torf == true).Count(); }
		public static int Deaths { get => Main.LevelManager.deaths; }
		public static string LevelInfo
		{
			get
			{
				if (Main.LevelManager != null)
					return $"{MoustacheCount.PercentageOf(MoustacheQuota)}% of Staches Collected, {Deaths} {(Deaths == 1 ? "Death" : "Deaths")}, {BubbaTokens} bubbas.";
				return "Starting";
			}
		}
		public static DiscordRpc.RichPresence CurrentPresence
		{
			get
			{
				var lvl = Level;
				var LargeImageKey = keyByLevel.GetOrDefault(lvl, "");
				var LargeImageText = lvl.ToString().Split("_".ToCharArray()).Join(" ").ToLower().ToTitleCase();
				if (lvl == DONT_DESTROY_ON_LOAD)
				{
					LargeImageKey = keyByLevel.GetOrDefault(Level.MAIN_MENU, "");
					LargeImageText = nameof(DONT_DESTROY_ON_LOAD).Split("_".ToCharArray()).Join(" ").ToLower().ToTitleCase();
				}
				var chr = Character;
				var SmallImageKey = keyByCharacter.GetOrDefault(chr, "");
				string chrName = chr.ToFriendlyName();
				if (chrName.Equals("BEEG"))
				{
					SmallImageKey = "amewide";
					chrName = "AMELIA_BEEG";
				}
				var SmallImageText = "Playing as " + chrName.Reverse("_", " ").ToLower().ToTitleCase();
				var levelTime = LevelTime;
				long? startTime = levelTime.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - (long)levelTime.Value.TotalMilliseconds).ToUnixTimeSeconds() : 0;
				return MainScript.paused ? new DiscordRpc.RichPresence
				{
					details = (Level == Level.MAIN_MENU ? "On the Main Menu" : $"Playing in {LargeImageText}"),
					state = "Paused",
					button1 = PlayButton,
					largeImageKey = LargeImageKey,
					largeImageText = LargeImageText,
					smallImageKey = SmallImageKey,
					smallImageText = SmallImageText,
					instance = true
				} : new DiscordRpc.RichPresence
				{
					details = (Level == Level.MAIN_MENU ? "On the Main Menu" : $"Playing in {LargeImageText}"),
					state = (Level == Level.MAIN_MENU ? "Roaming" : LevelInfo),
					startTimestamp = startTime.GetValueOrDefault(),
					endTimestamp = 0,
					button1 = PlayButton,
					largeImageKey = LargeImageKey,
					largeImageText = LargeImageText,
					smallImageKey = SmallImageKey,
					smallImageText = SmallImageText,
					instance = true
				};
			}
		}

		static Main()
		{
			try
			{
				PlayButton = new DiscordRpc.Button("Play", "https://kevincow.itch.io/smol-ame");//DiscordRpc.CreateButton("Play", "https://kevincow.itch.io/smol-ame");
				SALT.Console.Console.LogWarning(PlayButton.ToString());
			}
			catch (EntryPointNotFoundException ex)
			{
				SALT.Console.Console.LogError(ex.ParseTrace());
			}
		}

		public override void PreLoad()
		{
			execAssembly = Assembly.GetExecutingAssembly();
			HarmonyInstance.PatchAll(execAssembly);
		}

		private static DiscordRpc.Button PlayButton;

		public override void Load()
		{
			SALT.Main.mainScript.AddComponent<Director>();
			DiscordRpc.EventHandlers handlers = new DiscordRpc.EventHandlers();
			handlers.readyCallback = ReadyCallback;
			handlers.disconnectedCallback += DisconnectedCallback;
			handlers.errorCallback += ErrorCallback;

			DiscordRpc.Initialize(CLIENT_ID, ref handlers, true, null);
		}

		public override void PostLoad()
		{
			try
			{
				UpdateStatus();
				Callbacks.OnMainMenuLoaded += UpdateStatus;
				Callbacks.OnLevelLoaded += UpdateStatus;
			}
			catch (Exception ex)
			{
				SALT.Console.Console.LogError(ex.ParseTrace());
			}
		}

		internal static void ReadyCallback(ref DiscordRpc.DiscordUser user)
		{
			SALT.Console.Console.Log(string.Format("Got ready callback with user {0}#{1}", user.username, user.discriminator));
		}

		internal static void DisconnectedCallback(int errorCode, string message)
		{
			SALT.Console.Console.Log(string.Format("Got disconnect {0}: {1}", errorCode, message));
		}

		internal static void ErrorCallback(int errorCode, string message)
		{
			SALT.Console.Console.Log(string.Format("Got error {0}: {1}", errorCode, message));
		}

		public static bool Unload()
		{
			DiscordRpc.Shutdown();
			return true;
		}

		internal static void UpdateStatus()
		{
			DiscordRpc.UpdatePresence(CurrentPresence);
		}

		internal static void TurnOff()
		{
			DiscordRpc.RichPresence presence = new DiscordRpc.RichPresence
			{
				details = "",
				state = "",
				startTimestamp = 0,
				endTimestamp = 0,
				largeImageKey = "ame_head",
				largeImageText = "",
				smallImageKey = "",
				smallImageText = ""
			};

			DiscordRpc.UpdatePresence(presence);
		}

		internal static long UnixTime()
		{
			return (long)(DateTime.UtcNow - EPOCH).TotalSeconds;
		}

		private static Dictionary<Level, string> keyByLevel => new Dictionary<Level, string>
		{
			{
				Level.MAIN_MENU,
				"ame_head"
			},
			{
				Level.OFFICE,
				"office"
			},
			{
				Level.POP_ON_ROCKS,
				"clock"
			},
			{
				Level.RED_HEART,
				"tarantula"
			},
			{
				Level.PEKO_LAND,
				"pekoland"
			},
			{
				Level.OFFICE_REVERSED,
				"eurobeat"
			},
			{
				Level.TO_THE_MOON,
				"moon"
			},
			{
				Level.NOTHING,
				"nothing"
			},
			{
				Level.MOGU_MOGU,
				"mogu"
			},
			{
				Level.INUMORE,
				"statue"
			},
			{
				Level.RUSHIA,
				"rushia"
			},
			{
				Level.INASCAPABLE_MADNESS,
				"tako"
			},
			{
				Level.HERE_COMES_HOPE,
				"hopegrey"
			},
			{
				Level.REFLECT,
				"reflect"
			}
		};

		private static Dictionary<Character, string> keyByCharacter => new Dictionary<Character, string>
		{
			{
				Character.AMELIA,
				"bubba"
			},
			{
				Character.GURA,
				"bloop"
			},
			{
				Character.KORONE,
				"hosoinu"
			},
			{
				Character.OKAYU,
				"onigiri"
			},
			{
				Character.AMELIA_MOUSTACHE,
				"stache"
			},
			{
				Character.GURA_BUFF,
				"anchor"
			},
			{
				Character.KEVIN,
				"ushi"
			},
			{
				Character.SHUBA,
				"shuba"
			},
			{
				Character.GURA_CAT,
				"cat"
			},
			{
				Character.COCO,
				"cocohd"
			},
			{
				Character.OLLIE,
				"ollie"
			},
			{
				Character.REINE,
				"reine"
			},
			{
				Character.NONE,
				""
			}
		};
	}

	public class Director : MonoBehaviour
	{
		float lastUpdate;
		void Update()
		{
			float currentTime = Time.realtimeSinceStartup;
			if (currentTime > (lastUpdate + 0.25f))
			{
				lastUpdate = currentTime;
				Main.UpdateStatus();
			}
		}
		void OnApplicationQuit() => Main.Unload();
	}
}
