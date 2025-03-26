using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine.Serialization;
using System.Linq;
using UnityEngine;

namespace UnityEngine.UI.Extensions
{
    /// <summary>
    /// Displays a Texture2D for the UI System.
    /// </summary>
    /// <remarks>
    /// If you don't have or don't wish to create an atlas, you can simply use this script to draw a texture.
    /// Keep in mind though that this will create an extra draw call with each RawImage present, so it's
    /// best to use it only for backgrounds or temporary visible graphics.
    /// </remarks>

    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/Polygon", 12)]
    public class UIPoly : MaskableGraphic
    {
        [FormerlySerializedAs("m_Tex")]
        [SerializeField] Texture m_Texture;
        [SerializeField] Rect m_UVRect = new Rect(0f, 0f, 1f, 1f);

        //protected RawImage()
        //{
        //    useLegacyMeshGeneration = false;
        //}

        /// <summary>
        /// Returns the texture used to draw this Graphic.
        /// </summary>
        public override Texture mainTexture
        {
            get
            {
                if (m_Texture == null)
                {
                    if (material != null && material.mainTexture != null)
                    {
                        return material.mainTexture;
                    }
                    return s_WhiteTexture;
                }

                return m_Texture;
            }
        }

        /// <summary>
        /// The RawImage's texture to be used.
        /// </summary>
        /// <remarks>
        /// Use this to alter or return the Texture the RawImage displays. The Raw Image can display any Texture whereas an Image component can only show a Sprite Texture.
        /// Note : Keep in mind that using a RawImage creates an extra draw call with each RawImage present, so it's best to use it only for backgrounds or temporary visible graphics.Note: Keep in mind that using a RawImage creates an extra draw call with each RawImage present, so it's best to use it only for backgrounds or temporary visible graphics.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// //Create a new RawImage by going to Create>UI>Raw Image in the hierarchy.
        /// //Attach this script to the RawImage GameObject.
        ///
        /// using UnityEngine;
        /// using UnityEngine.UI;
        ///
        /// public class RawImageTexture : MonoBehaviour
        /// {
        ///     RawImage m_RawImage;
        ///     //Select a Texture in the Inspector to change to
        ///     public Texture m_Texture;
        ///
        ///     void Start()
        ///     {
        ///         //Fetch the RawImage component from the GameObject
        ///         m_RawImage = GetComponent<RawImage>();
        ///         //Change the Texture to be the one you define in the Inspector
        ///         m_RawImage.texture = m_Texture;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Texture texture
        {
            get
            {
                return m_Texture;
            }
            set
            {
                if (m_Texture == value)
                    return;

                m_Texture = value;
                SetVerticesDirty();
                SetMaterialDirty();
            }
        }

        /// <summary>
        /// UV rectangle used by the texture.
        /// </summary>
        public Rect uvRect
        {
            get
            {
                return m_UVRect;
            }
            set
            {
                if (m_UVRect == value)
                    return;
                m_UVRect = value;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Adjust the scale of the Graphic to make it pixel-perfect.
        /// </summary>
        /// <remarks>
        /// This means setting the RawImage's RectTransform.sizeDelta  to be equal to the Texture dimensions.
        /// </remarks>
        public override void SetNativeSize()
        {
            Texture tex = mainTexture;
            if (tex != null)
            {
                int w = Mathf.RoundToInt(tex.width * uvRect.width);
                int h = Mathf.RoundToInt(tex.height * uvRect.height);
                rectTransform.anchorMax = rectTransform.anchorMin;
                rectTransform.sizeDelta = new Vector2(w, h);
            }
        }

        [SerializeField] List<Vector2> m_Verticies = new List<Vector2>();
        public List<Vector2> m_Verts
        {
            get
            {
                return m_Verticies;
            }
            set
            {
                m_Verts = value;
            }
        }
        public List<Vector2> m_VertsWS
        {
            get
            {
                List<Vector2> verts = new();
                foreach (var v in m_Verticies)
                {
                    verts.Add(new Vector2(rectTransform.rect.center.x + rectTransform.rect.width * v.x, rectTransform.rect.center.y + rectTransform.rect.height * v.y));
                }
                return verts;
            }
            set
            {
                List<Vector2> verts = new();
                foreach (var v in value)
                {
                    verts.Add(new Vector2(v.x / rectTransform.rect.width - rectTransform.rect.center.x, v.y / rectTransform.rect.height - rectTransform.rect.center.y));
                }
                m_Verticies = verts;
            }
        }

        public List<UnityEngine.Vector3Int> tris = new List<UnityEngine.Vector3Int>();
        List<Vector2> uv => m_Verticies;
        public UnityEngine.Vector3Int vector;
        //public List<int> triangles = new List<int>();
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            Texture tex = mainTexture;
            vh.Clear();
            if (tex != null)
            {
                List<Vector2> verts = m_VertsWS;


                //var clockwiseVerts = OrderClockwise(verticies, Average(verticies));

                //var averageVerts = AverageAdded(clockwiseVerts);
                //var vertArray = averageVerts.ToArray();
                //var uv = UV(averageVerts);
                tris = Triangles(verts.Count);
                for (int i = 0; i < verts.Count; i++)
                {
                    var color32 = color;
                    vh.AddVert(verts[i], color32, uv[i] + Vector2.one / 2);
                }

                //var tris = Triangles(verticies.Count);
                //triangles = tris;
                for (int i = 0; i < tris.Count; i++)
                {
                    vh.AddTriangle(tris[i].x, tris[i].y, tris[i].z);
                }



                //var r = rect;//GetPixelAdjustedRect();
                //var v = new Vector4(r.x, r.y, r.x + r.width, r.y + r.height);
                //var scaleX = tex.width * tex.texelSize.x;
                //var scaleY = tex.height * tex.texelSize.y;
                //{
                //    var color32 = color;
                //    vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(m_UVRect.xMin * scaleX, m_UVRect.yMin * scaleY));
                //    vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(m_UVRect.xMin * scaleX, m_UVRect.yMax * scaleY));
                //    vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(m_UVRect.xMax * scaleX, m_UVRect.yMax * scaleY));
                //    vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(m_UVRect.xMax * scaleX, m_UVRect.yMin * scaleY));

                //    vh.AddTriangle(0, 1, 2);
                //    vh.AddTriangle(2, 3, 0);
                //}


            }
        }


        #region Polygon
        Vector3 Average(List<Vector3> vector3s)
        {
            int count = 0;
            Vector3 sum = Vector3.zero;
            foreach (Vector3 vert in vector3s)
            {
                sum += vert;
                count++;
            }
            Vector3 average = sum / (float)count;
            return average;
        }
        List<Vector3> AverageAdded(List<Vector3> vector3s)
        {
            List<Vector3> averageFirst = new List<Vector3>();
            averageFirst.Add(Average(vector3s));
            averageFirst.AddRange(vector3s);
            return averageFirst;
        }
        List<Vector3> OrderClockwise(List<Vector3> vector3s, Vector3 center)
        {
            //https://stackoverflow.com/questions/6880899/sort-a-set-of-3-d-points-in-clockwise-counter-clockwise-order
            //https://en.wikipedia.org/wiki/Atan2
            vector3s = vector3s.OrderBy(i => Mathf.Atan2(center.z - i.z, center.x - i.x)).ToList();
            vector3s.Reverse();
            return vector3s;
        }


        List<Vector3Int> Triangles(int sides)
        {
            //center
            //this
            //next
            List<Vector3Int> tris = new();

            if (sides == 3)
            {
                tris.Add(new Vector3Int(0, 1, 2));
                return tris;
            }
            if (sides == 4)
            {
                tris.Add(new Vector3Int(0, 1, 2));
                tris.Add(new Vector3Int(0, 2, 3));
                return tris;
            }
            if (sides == 5)
            {
                tris.Add(new Vector3Int(0, 1, 2));
                tris.Add(new Vector3Int(0, 2, 3));
                tris.Add(new Vector3Int(0, 3, 4));
                return tris;
            }
            for (int i = 0; i < sides; i++)
            {
                Vector3Int tri = new();
                tri.x = 0;
                tri.y = i;
                if (i + 1 >= sides)
                {
                    tri.z = 1;
                }
                else
                {
                    tri.z = i + 1;
                }
                tris.Add(tri);
            }
            return tris;
        }

        List<Vector2> UV(List<Vector3> verticies)
        {
            List<Vector2> uvs = new List<Vector2>();
            foreach (Vector3 vert in verticies)
            {
                uvs.Add(new Vector2(vert.x, vert.y));
            }
            return uvs;
        }
        #endregion

        protected override void OnDidApplyAnimationProperties()
        {
            SetMaterialDirty();
            SetVerticesDirty();
            SetRaycastDirty();
        }
    }
}
