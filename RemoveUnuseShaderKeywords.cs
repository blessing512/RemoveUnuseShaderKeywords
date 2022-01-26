using AssetBundles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace StickGame
{
    class RemoveUnuseShaderKeywords
    {

        public static String DEFAULT_PATH = "Assets/AssetsPackage";

        static Dictionary<string, string> dicAllKeywrods = new Dictionary<string, string>();

        #region FindMaterial

        static List<string> allShaderNameList = new List<string>();


        static public void CollectionAllShaderKeywords()
        {

            foreach(var item in allShaderNameList)
            {
                var shaderItem = AssetDatabase.LoadAssetAtPath<Shader>(item);
                if(shaderItem)
                {
                    ShaderData shaderData = GetShaderKeywords(shaderItem);
                    if(shaderData != null)
                    {

                        //求所有mat的kw
                        for (int i = 0; i < shaderData.PassTypes.Length; i++)
                        {
                            //
                            var pt = (PassType)shaderData.PassTypes[i];
                  

                            for (int l = 0; l < shaderData.KeyWords[i].Length; l++)
                            {
                                string keyword = shaderData.KeyWords[i][l];
                                dicAllKeywrods[keyword] = keyword;
                            }
                        }

                        for (int i = 0; i < shaderData.ReMainingKeyWords.Length - 1; i++)
                        {
                            string keyword = shaderData.ReMainingKeyWords[i];
                            dicAllKeywrods[keyword] = keyword;
                        }

                    }
                }

            }


        }

        static List<string> lsTempKeyWords = new List<string>();

        [MenuItem("打包工具/AssetBundles/删除无用shaderkeyword")]
        public static void RemoveUnuserShderKeywords()
        {
      
            //先搜集所有keyword到工具类SVC
            toolSVC = new ShaderVariantCollection();

            var shaders = AssetDatabase.FindAssets("t:Shader", new string[] { "Assets", "Packages" }).ToList();
            foreach (var shader in shaders)
            {
                ShaderVariantCollection.ShaderVariant sv = new ShaderVariantCollection.ShaderVariant();
                var shaderPath = AssetDatabase.GUIDToAssetPath(shader);


                sv.shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                toolSVC.Add(sv);
                //
                allShaderNameList.Add(shaderPath);
            }


            CollectionAllShaderKeywords();

            //搜索所有Mat
            List<string> lsPaths = new List<string>();
            lsPaths.Add(DEFAULT_PATH);
            var assets = AssetDatabase.FindAssets("t:Material", lsPaths.ToArray()).ToList();

            List<string> allMats = new List<string>();

            //GUID to assetPath
            for (int i = 0; i < assets.Count; i++)
            {
                var p = AssetDatabase.GUIDToAssetPath(assets[i]);
                //获取依赖中的mat
                var dependenciesPath = AssetDatabase.GetDependencies(p, true);
                var mats = dependenciesPath.ToList().FindAll((dp) => dp.EndsWith(".mat"));
                allMats.AddRange(mats);
            }

            //处理所有的 material
            allMats = allMats.Distinct().ToList();
            //allMats.Add("Assets/AssetsPackage/art/Effects Library/Material/smoke_035.mat");

            float count = 1;
            foreach (var mat in allMats)
            {
                lsTempKeyWords.Clear();
                var obj = AssetDatabase.LoadMainAssetAtPath(mat);
                if (obj is Material && !mat.Contains("/Editor/") && !mat.Contains("/testStuff/") && !mat.Contains("/Shader/Test/"))
                {
                    var _mat = obj as Material;

                    for(int i=0; i<_mat.shaderKeywords.Length;i++)
                    {
                        string strKeyword = _mat.shaderKeywords[i];
                        if (dicAllKeywrods.ContainsKey(strKeyword))
                        {
                            lsTempKeyWords.Add(strKeyword);
                        }
                        else
                        {
                            Debug.Log("删除无用keyword：" + strKeyword);
                        }
                    }


                    _mat.shaderKeywords = lsTempKeyWords.ToArray();

                    EditorUtility.DisplayProgressBar("处理mat", string.Format("处理:{0} - {1}", Path.GetFileName(mat), _mat.shader.name), count / allMats.Count);
      

                }

                count++;
            }

            EditorUtility.ClearProgressBar();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

   
        }


        public class ShaderData
        {
            public int[] PassTypes = new int[] { };
            public string[][] KeyWords = new string[][] { };
            public string[] ReMainingKeyWords = new string[] { };
        }




        static MethodInfo GetShaderVariantEntries = null;

        static ShaderVariantCollection toolSVC = null;

        //获取shader的 keywords
        public static ShaderData GetShaderKeywords(Shader shader)
        {
            ShaderData sd = new ShaderData();
            GetShaderVariantEntriesFiltered(shader, new string[] { }, out sd.PassTypes, out sd.KeyWords, out sd.ReMainingKeyWords);
            return sd;
        }

        /// <summary>
        /// 获取keyword
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="filterKeywords"></param>
        /// <param name="passTypes"></param>
        /// <param name="keywordLists"></param>
        /// <param name="remainingKeywords"></param>
        static void GetShaderVariantEntriesFiltered(Shader shader, string[] filterKeywords, out int[] passTypes, out string[][] keywordLists, out string[] remainingKeywords)
        {
            //2019.3接口
            //            internal static void GetShaderVariantEntriesFiltered(
            //                Shader                  shader,                     0
            //                int                     maxEntries,                 1
            //                string[]                filterKeywords,             2
            //                ShaderVariantCollection excludeCollection,          3
            //                out int[]               passTypes,                  4
            //                out string[]            keywordLists,               5
            //                out string[]            remainingKeywords)          6
            if (GetShaderVariantEntries == null)
            {
                GetShaderVariantEntries = typeof(ShaderUtil).GetMethod("GetShaderVariantEntriesFiltered", BindingFlags.NonPublic | BindingFlags.Static);
            }

            passTypes = new int[] { };
            keywordLists = new string[][] { };
            remainingKeywords = new string[] { };
            if (toolSVC != null)
            {
                var _passtypes = new int[] { };
                var _keywords = new string[] { };
                var _remainingKeywords = new string[] { };
                object[] args = new object[] { shader, 65536, filterKeywords, toolSVC, _passtypes, _keywords, _remainingKeywords };
                GetShaderVariantEntries.Invoke(null, args);

                var passtypes = args[4] as int[];
                passTypes = passtypes;
                //key word
                keywordLists = new string[passtypes.Length][];
                var kws = args[5] as string[];
                for (int i = 0; i < passtypes.Length; i++)
                {
                    keywordLists[i] = kws[i].Split(' ');
                }

                //Remaning key word
                var rnkws = args[6] as string[];
                remainingKeywords = rnkws;
            }
        }

        #endregion

    }
}
