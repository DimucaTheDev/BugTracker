namespace Website.Util;
/// <summary>
/// Этот аттрибут нужен шоб редеректела(
/// стандартный [Authorize] ваще прост кидает 401, а мне надо что бы он еще и на /логин кидал..... asp.net 1 💖
/// </summary>
//[AttributeUsage(validOn: AttributeTargets.Delegate | AttributeTargets.Method | AttributeTargets.GenericParameter)]
public class Auth : Attribute
{
    public string? Roles;
}