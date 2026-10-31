namespace UnityEngine { public class AudioClip {} public class Texture2D {} public class Sprite {} }
namespace UnityEngine
{
 public struct Vector3(float x, float y, float z)
 {
  public float x = x, y = y, z = z;
  public static Vector3 zero => new(0,0,0);
  public static Vector3 one => new(1,1,1);
 }
}
namespace Il2Cpp
{
 public class WeaponScript { public enum DOEEBEFKIJI { MACHINE_GUN,GRENADE_LAUNCHER,SHOTGUN,KNIFE } }
 public class LobbyMenu { public class AllMaps { public string mapName; } }
}
namespace CustomMapsFramework
{
 internal static class MapContent
 {
  internal static bool Name(string value)=>!string.IsNullOrWhiteSpace(value)&&value.Length<=128&&!value.Any(char.IsControl);
 }
}
