using UnityEngine;
namespace BackpackRTS {public sealed class RuntimeMesh:MonoBehaviour {public Mesh mesh;void OnDestroy(){if(mesh!=null)Destroy(mesh);}}}
