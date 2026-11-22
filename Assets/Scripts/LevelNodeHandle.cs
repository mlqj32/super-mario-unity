using UnityEngine;

namespace Mario
{
    // Used by the level editor's native scene preview. Runtime reads LevelData directly.
    public sealed class LevelNodeHandle : MonoBehaviour
    {
        public int sourceIndex;
        public NodeData node;
        void OnDrawGizmosSelected()
        {
            if(node==null)return;
            Gizmos.color=node.group=="Character"?Color.red:Color.yellow;
            Gizmos.DrawWireCube(transform.position+new Vector3(node.width/2,node.height/2),new Vector3(node.width,node.height,0));
        }
    }
}
