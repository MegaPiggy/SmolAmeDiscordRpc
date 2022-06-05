using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using SALT.Extensions;
using RichPresence;

public class DiscordRpc
{
    [MonoPInvokeCallback(typeof(OnReadyInfo))]
    public static void ReadyCallback(ref DiscordUser connectedUser) { Callbacks.readyCallback(ref connectedUser); }
    public delegate void OnReadyInfo(ref DiscordUser connectedUser);

    [MonoPInvokeCallback(typeof(OnDisconnectedInfo))]
    public static void DisconnectedCallback(int errorCode, string message) { Callbacks.disconnectedCallback(errorCode, message); }
    public delegate void OnDisconnectedInfo(int errorCode, string message);

    [MonoPInvokeCallback(typeof(OnErrorInfo))]
    public static void ErrorCallback(int errorCode, string message) { Callbacks.errorCallback(errorCode, message); }
    public delegate void OnErrorInfo(int errorCode, string message);

    [MonoPInvokeCallback(typeof(OnJoinInfo))]
    public static void JoinCallback(string secret) { Callbacks.joinCallback(secret); }
    public delegate void OnJoinInfo(string secret);

    [MonoPInvokeCallback(typeof(OnSpectateInfo))]
    public static void SpectateCallback(string secret) { Callbacks.spectateCallback(secret); }
    public delegate void OnSpectateInfo(string secret);

    [MonoPInvokeCallback(typeof(OnRequestInfo))]
    public static void RequestCallback(ref DiscordUser request) { Callbacks.requestCallback(ref request); }
    public delegate void OnRequestInfo(ref DiscordUser request);

    static EventHandlers Callbacks { get; set; }

    public struct EventHandlers
    {
        public OnReadyInfo readyCallback;
        public OnDisconnectedInfo disconnectedCallback;
        public OnErrorInfo errorCallback;
        public OnJoinInfo joinCallback;
        public OnSpectateInfo spectateCallback;
        public OnRequestInfo requestCallback;
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct RichPresenceStruct
    {
        public IntPtr state; /* max 128 bytes */
        public IntPtr details; /* max 128 bytes */
        public long startTimestamp;
        public long endTimestamp;
        public IntPtr largeImageKey; /* max 32 bytes */
        public IntPtr largeImageText; /* max 128 bytes */
        public IntPtr smallImageKey; /* max 32 bytes */
        public IntPtr smallImageText; /* max 128 bytes */
        public IntPtr partyId; /* max 128 bytes */
        public int partySize;
        public int partyMax;
        public int partyPrivacy;
        public IntPtr matchSecret; /* max 128 bytes */
        public IntPtr joinSecret; /* max 128 bytes */
        public IntPtr spectateSecret; /* max 128 bytes */
        public bool instance;
        public IntPtr button1label;
        public IntPtr button1url;
        public IntPtr button2label;
        public IntPtr button2url;
    }

    [Serializable]
    public struct DiscordUser
    {
        public string userId;
        public string username;
        public string discriminator;
        public string avatar;
    }

    public enum Reply
    {
        No = 0,
        Yes = 1,
        Ignore = 2
    }

    public enum PartyPrivacy
    {
        Private = 0,
        Public = 1
    }

    /// <summary>
    /// A Rich Presence button.
    /// </summary>
    [Serializable]
    public struct Button
    {
        /// <summary>
        /// Text shown on the button
        /// <para>Max 32 bytes.</para>
        /// </summary>
        public string label;

        /// <summary>
        /// The URL opened when clicking the button.
        /// <para>Max 512 bytes.</para>
        /// </summary>
        public string url { get => _url; set => _url = Verify(value); }
        private string _url;

        public static Button Default() => new Button
        {
            label = string.Empty,
            _url = string.Empty
        };

        public Button(string label, string url)
        {
            this.label = label;
            this._url = Verify(url);
        }

        /// <summary>
        /// Verifies that the url is a valid URI
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public static string Verify(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uriResult))
                return uriResult.ToString();
            throw new ArgumentException("Url must be a valid URI");
        }

        public override string ToString()
        {
            return $"[{label}, {url}]";
        }

        public bool IsEmpty() => string.IsNullOrEmpty(label) || string.IsNullOrEmpty(url);

        public Button Clone() => new Button { label = this.label, url = this.url };
    }

    public static void Initialize(string applicationId, ref EventHandlers handlers, bool autoRegister, string optionalSteamId)
    {
        Callbacks = handlers;

        EventHandlers staticEventHandlers = new EventHandlers();
        staticEventHandlers.readyCallback += DiscordRpc.ReadyCallback;
        staticEventHandlers.disconnectedCallback += DiscordRpc.DisconnectedCallback;
        staticEventHandlers.errorCallback += DiscordRpc.ErrorCallback;
        staticEventHandlers.joinCallback += DiscordRpc.JoinCallback;
        staticEventHandlers.spectateCallback += DiscordRpc.SpectateCallback;
        staticEventHandlers.requestCallback += DiscordRpc.RequestCallback;

        InitializeInternal(applicationId, ref staticEventHandlers, autoRegister, optionalSteamId);
    }

    [DllImport("discord-rpc", EntryPoint = "Discord_Initialize", CallingConvention = CallingConvention.Cdecl)]
    static extern void InitializeInternal(string applicationId, ref EventHandlers handlers, bool autoRegister, string optionalSteamId);

    [DllImport("discord-rpc", EntryPoint = "Discord_Register", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Register(string applicationId, string command);

    [DllImport("discord-rpc", EntryPoint = "Discord_RegisterSteamGame", CallingConvention = CallingConvention.Cdecl)]
    public static extern void RegisterSteamGame(string applicationId, string steamId);

    [DllImport("discord-rpc", EntryPoint = "Discord_Shutdown", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Shutdown();

    [DllImport("discord-rpc", EntryPoint = "Discord_RunCallbacks", CallingConvention = CallingConvention.Cdecl)]
    public static extern void RunCallbacks();

    [DllImport("discord-rpc", EntryPoint = "Discord_UpdatePresence", CallingConvention = CallingConvention.Cdecl)]
    private static extern void UpdatePresenceNative(ref RichPresenceStruct presence);

    [DllImport("discord-rpc", EntryPoint = "Discord_ClearPresence", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ClearPresence();

    [DllImport("discord-rpc", EntryPoint = "Discord_Respond", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Respond(string userId, Reply reply);

    [DllImport("discord-rpc", EntryPoint = "Discord_UpdateHandlers", CallingConvention = CallingConvention.Cdecl)]
    public static extern void UpdateHandlers(ref EventHandlers handlers);

    public static void UpdatePresence(RichPresence presence)
    {
        var presencestruct = presence.GetStruct();
        UpdatePresenceNative(ref presencestruct);
        presence.FreeMem();
    }

    public class RichPresence : IEquatable<RichPresence>
    {
        private RichPresenceStruct _presence;
        private readonly List<IntPtr> _buffers = new List<IntPtr>(10);

        public string state; /* max 128 bytes */
        public string details; /* max 128 bytes */
        public long startTimestamp;
        public long endTimestamp;
        public string largeImageKey; /* max 32 bytes */
        public string largeImageText; /* max 128 bytes */
        public string smallImageKey; /* max 32 bytes */
        public string smallImageText; /* max 128 bytes */
        public string partyId; /* max 128 bytes */
        public int partySize;
        public int partyMax;
        public PartyPrivacy partyPrivacy;
        public string matchSecret; /* max 128 bytes */
        public string joinSecret; /* max 128 bytes */
        public string spectateSecret; /* max 128 bytes */
        public bool instance;
        /// <summary>
        /// A button to display in the presence. 
        /// </summary>
        public Button button1 = Button.Default();
        /// <summary>
        /// A button to display in the presence. 
        /// </summary>
        public Button button2 = Button.Default();
        public Button[] buttons => new Button[2]
        {
            button1,
            button2
        };

        /// <summary>
        /// Get the <see cref="RichPresenceStruct"/> reprensentation of this instance
        /// </summary>
        /// <returns><see cref="RichPresenceStruct"/> reprensentation of this instance</returns>
        internal RichPresenceStruct GetStruct()
        {
            if (_buffers.Count > 0)
            {
                FreeMem();
            }

            _presence.state = StrToPtr(state);
            _presence.details = StrToPtr(details);
            _presence.startTimestamp = startTimestamp;
            _presence.endTimestamp = endTimestamp;
            _presence.largeImageKey = StrToPtr(largeImageKey);
            _presence.largeImageText = StrToPtr(largeImageText);
            _presence.smallImageKey = StrToPtr(smallImageKey);
            _presence.smallImageText = StrToPtr(smallImageText);
            _presence.partyId = StrToPtr(partyId);
            _presence.partySize = partySize;
            _presence.partyMax = partyMax;
            _presence.partyPrivacy = (int)partyPrivacy;
            _presence.matchSecret = StrToPtr(matchSecret);
            _presence.joinSecret = StrToPtr(joinSecret);
            _presence.spectateSecret = StrToPtr(spectateSecret);
            _presence.instance = instance;
            _presence.button1label = StrToPtr(button1.label);
            _presence.button1url = StrToPtr(button1.url);
            _presence.button2label = StrToPtr(button2.label);
            _presence.button2url = StrToPtr(button2.url);

            return _presence;
        }

        /// <summary>
        /// Returns a pointer to a representation of the given string with a size of maxbytes
        /// </summary>
        /// <param name="input">String to convert</param>
        /// <returns>Pointer to the UTF-8 representation of <see cref="input"/></returns>
        private IntPtr StrToPtr(string input)
        {
            if (string.IsNullOrEmpty(input)) return IntPtr.Zero;
            var convbytecnt = Encoding.UTF8.GetByteCount(input);
            var buffer = Marshal.AllocHGlobal(convbytecnt + 1);
            for (int i = 0; i < convbytecnt + 1; i++)
            {
                Marshal.WriteByte(buffer, i, 0);
            }
            _buffers.Add(buffer);
            Marshal.Copy(Encoding.UTF8.GetBytes(input), 0, buffer, convbytecnt);
            return buffer;
        }

        /// <summary>
        /// Convert string to UTF-8 and add null termination
        /// </summary>
        /// <param name="toconv">string to convert</param>
        /// <returns>UTF-8 representation of <see cref="toconv"/> with added null termination</returns>
        private static string StrToUtf8NullTerm(string toconv)
        {
            var str = toconv.Trim();
            var bytes = Encoding.Default.GetBytes(str);
            if (bytes.Length > 0 && bytes[bytes.Length - 1] != 0)
            {
                str += "\0\0";
            }
            return Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        /// Free the allocated memory for conversion to <see cref="RichPresenceStruct"/>
        /// </summary>
        internal void FreeMem()
        {
            for (var i = _buffers.Count - 1; i >= 0; i--)
            {
                Marshal.FreeHGlobal(_buffers[i]);
                _buffers.RemoveAt(i);
            }
        }

        /// <summary>
        /// Does the Rich Presence have valid timestamps?
        /// </summary>
        /// <returns></returns>
        public bool HasTimestamps()
        {
            return this.startTimestamp != 0 || this.endTimestamp != 0;
        }

        /// <summary>
        /// Does the Rich Presence have valid assets?
        /// </summary>
        /// <returns></returns>
        public bool HasAssets()
        {
            return !string.IsNullOrWhiteSpace(largeImageKey) || !string.IsNullOrWhiteSpace(largeImageText) || !string.IsNullOrWhiteSpace(smallImageKey) || !string.IsNullOrWhiteSpace(smallImageText);
        }

        /// <summary>
        /// Does the Rich Presence have a valid party?
        /// </summary>
        /// <returns></returns>
        public bool HasParty()
        {
            return !string.IsNullOrWhiteSpace(partyId);
        }

        /// <summary>
        /// Does the Rich Presence have valid secrets?
        /// </summary>
        /// <returns></returns>
        public bool HasSecrets()
        {
            return !string.IsNullOrWhiteSpace(joinSecret) || !string.IsNullOrWhiteSpace(spectateSecret);
        }

        /// <summary>
        /// Does the Rich Presence have any buttons?
        /// </summary>
        /// <returns></returns>
        public bool HasButtons()
        {
            return buttons != null && buttons.Length > 0;
        }

        /// <summary>
        /// Merges the passed presence with this one, taking into account the image key to image id annoyance.
        /// </summary>
        /// <param name="presence"></param>
        /// <returns>A new presence with the merged properties of both.</returns>
        internal RichPresence Merge(RichPresence presence)
        {
            RichPresence merged = new RichPresence();
            merged.state = presence.state.IsNullOrWhiteSpace() ? this.state : presence.state;
            merged.details = presence.details.IsNullOrWhiteSpace() ? this.details : presence.details;
            merged.partyId = presence.partyId.IsNullOrWhiteSpace() ? this.partyId : presence.partyId;
            merged.partySize = presence.partySize == 0 ? this.partySize : presence.partySize;
            merged.partyMax = presence.partyMax == 0 ? this.partyMax : presence.partyMax;
            merged.partyPrivacy = presence.partyPrivacy == PartyPrivacy.Private ? this.partyPrivacy : presence.partyPrivacy;
            merged.startTimestamp = presence.startTimestamp == 0 ? this.startTimestamp : presence.startTimestamp;
            merged.endTimestamp = presence.endTimestamp == 0 ? this.endTimestamp : presence.endTimestamp;
            merged.joinSecret = presence.joinSecret.IsNullOrWhiteSpace() ? this.joinSecret : presence.joinSecret;
            merged.spectateSecret = presence.spectateSecret.IsNullOrWhiteSpace() ? this.spectateSecret : presence.spectateSecret;
            merged.largeImageKey = presence.largeImageKey.IsNullOrWhiteSpace() ? this.largeImageKey : presence.largeImageKey;
            merged.largeImageText = presence.largeImageText.IsNullOrWhiteSpace() ? this.largeImageText : presence.largeImageText;
            merged.smallImageKey = presence.smallImageKey.IsNullOrWhiteSpace() ? this.smallImageKey : presence.smallImageKey;
            merged.smallImageText = presence.smallImageText.IsNullOrWhiteSpace() ? this.smallImageText : presence.smallImageText;
            merged.button1 = presence.button1.IsEmpty() ? this.button1 : presence.button1;
            merged.button2 = presence.button2.IsEmpty() ? this.button2 : presence.button2;
            if (merged.button2.IsEmpty() && !this.button1.IsEmpty())
                merged.button2 = this.button1;
            return merged;
        }

        public bool Matches(RichPresence other)
        {
            if (other == null)
                return false;

            if (state != other.state || details != other.details)
                return false;

            //Checks if the timestamps are different
            if (startTimestamp != other.startTimestamp || endTimestamp != other.endTimestamp)
                return false;

            //Checks if the secrets are different
            if (joinSecret != other.joinSecret || matchSecret != other.matchSecret || spectateSecret != other.spectateSecret)
                return false;

            //Checks if the parties are different
            if (partyId != other.partyId ||
                partyMax != other.partyMax ||
                partySize != other.partySize ||
                partyPrivacy != other.partyPrivacy)
                return false;

            //Checks if the assets are different
            if (largeImageKey != other.largeImageKey ||
                largeImageText != other.largeImageText ||
                smallImageKey != other.smallImageKey ||
                smallImageText != other.smallImageText)
                return false;

            if (instance != other.instance)
                return false;

            //Check buttons
            if (buttons == null ^ other.buttons == null) return false;
            if (buttons != null)
            {
                if (buttons.Length != other.buttons.Length) return false;
                for (int i = 0; i < buttons.Length; i++)
                {
                    var a = buttons[i];
                    var b = other.buttons[i];
                    if (a.label != b.label || a.url != b.url)
                        return false;
                }
            }

            return true;
        }

        public bool Equals(RichPresence other) => Matches(other);

        public override bool Equals(object obj)
        {
            if (obj is RichPresence other)
                return Equals(other);
            return false;
        }

        /// <summary>
        /// Operator that converts a presence into a boolean for null checks.
        /// </summary>
        /// <param name="presesnce"></param>
        public static explicit operator bool(RichPresence presesnce)
        {
            return presesnce != null;
        }

        public override string ToString() => base.ToString();

        public override int GetHashCode() => base.GetHashCode();
    }
}