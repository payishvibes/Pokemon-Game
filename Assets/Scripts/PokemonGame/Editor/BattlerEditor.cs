using System.Reflection;
using UnityEngine;

namespace PokemonGame.Editor
{
    using UnityEditor;
    using General;
    
    [CustomEditor(typeof(Battler))]
    public class BattlerEditor : Editor
    {
        public override Texture2D RenderStaticPreview(string assetPath,Object[] subAssets,int width,int height)
        {
            Battler battlerTemplate = (Battler)target;
            
            if(battlerTemplate.GetSpriteFront()!=null)
            {
                System.Type t = GetType("UnityEditor.SpriteUtility");
                if(t!=null)
                {
                    MethodInfo method = t.GetMethod("RenderStaticPreview",new System.Type[] { typeof(Sprite),typeof(Color),typeof(int),typeof(int) });
                    if(method!=null)
                    {
                        object ret = method.Invoke("RenderStaticPreview",new object[] { battlerTemplate.GetSpriteFront(),Color.white,width,height });
                        if(ret is Texture2D)
                            return ret as Texture2D;
                    }
                }
            }
            return base.RenderStaticPreview(assetPath,subAssets,width,height);
        }
        
        private static System.Type GetType(string TypeName)
        {
            var type = System.Type.GetType(TypeName);
            if(type!=null)
                return type;

            if(TypeName.Contains("."))
            {
                var assemblyName = TypeName.Substring(0,TypeName.IndexOf('.'));
                var assembly = Assembly.Load(assemblyName);
                if(assembly==null)
                    return null;
                type=assembly.GetType(TypeName);
                if(type!=null)
                    return type;
            }

            var currentAssembly = Assembly.GetExecutingAssembly();
            var referencedAssemblies = currentAssembly.GetReferencedAssemblies();
            foreach(var assemblyName in referencedAssemblies)
            {
                var assembly = Assembly.Load(assemblyName);
                if(assembly!=null)
                {
                    type=assembly.GetType(TypeName);
                    if(type!=null)
                        return type;
                }
            }
            return null;
        }
    }
}