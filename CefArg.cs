namespace SampSharp.Cef.Entities;

/// <summary>Типы аргументов, поддерживаемые CEF-мостом между сервером и JS.</summary>
public enum CefArgType : byte
{
    String = 0,
    Integer = 1,
    Float = 2,
    Bool = 3,
}

/// <summary>
/// Tagged union для значения, передаваемого JS ↔ сервер. Используется в
/// <see cref="ICefService.EmitEvent"/> и в коллбеках входящих JS-событий.
/// </summary>
public readonly struct CefArg
{
    public CefArgType Type { get; }
    public string? StringValue { get; }
    public int IntValue { get; }
    public float FloatValue { get; }
    public bool BoolValue { get; }

    private CefArg(CefArgType type, string? s, int i, float f, bool b)
    {
        Type = type;
        StringValue = s;
        IntValue = i;
        FloatValue = f;
        BoolValue = b;
    }

    public static CefArg Str(string value) => new(CefArgType.String, value, 0, 0f, false);
    public static CefArg Int(int value) => new(CefArgType.Integer, null, value, 0f, false);
    public static CefArg Float(float value) => new(CefArgType.Float, null, 0, value, false);
    public static CefArg Bool(bool value) => new(CefArgType.Bool, null, 0, 0f, value);

    public static implicit operator CefArg(string v) => Str(v);
    public static implicit operator CefArg(int v) => Int(v);
    public static implicit operator CefArg(float v) => Float(v);
    public static implicit operator CefArg(bool v) => Bool(v);

    public override string ToString() => Type switch
    {
        CefArgType.String => $"\"{StringValue}\"",
        CefArgType.Integer => IntValue.ToString(),
        CefArgType.Float => FloatValue.ToString("R"),
        CefArgType.Bool => BoolValue.ToString(),
        _ => "?",
    };
}

/// <summary>Коды причин завершения CEF-handshake (mirrors <c>E_CEF_INIT_REASON</c>).</summary>
public enum CefInitReason
{
    Ok = 0,
    Timeout = 1,
    VersionMismatch = 2,
    IpMismatch = 3,
    HandshakeFailed = 4,
    Unknown = 5,
}

/// <summary>Коды ответа на создание браузера (mirrors <c>E_CEF_CREATE_STATUS</c>).</summary>
public enum CefCreateStatus
{
    Success = 0,
    ErrorGeneric = 1,
    ErrorIdAlreadyInUse = 2,
}

/// <summary>Режим воспроизведения звука браузера (mirrors <c>E_CEF_AUDIO_MODE</c>).</summary>
public enum CefAudioMode
{
    World = 0,
    Ui = 1,
}

/// <summary>Компоненты HUD'а, которые можно скрывать через CEF (mirrors <c>E_HUD_COMPONENT</c>).</summary>
public enum CefHudComponent
{
    All = 0,
    Ammo = 1,
    Armour = 2,
    Breath = 3,
    Crosshair = 4,
    Health = 5,
    Money = 6,
    Radar = 7,
    WantedStars = 8,
    Weapon = 9,
}
