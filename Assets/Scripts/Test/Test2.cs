//UTF-8格式
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
/// <summary>
/// 构建 管线
/// </summary>
public class ManifestBuilder
{

	//临时存储结构
	public class BuildAssetInfo
	{
		public string Address;
		public string AssetPath;
		public string BundleName;
		public List<int> DependIds;
	}
	public class BuildBundleInfo
	{
		public string BundleName;
		public string OutputFilePath;
		public List<string> Dependencies;
		public List<string> Assets;//包含的资源路径？？？	
	}
	//TODO:序列化成三份json文件
	public Manifest Build(List<BuildBundleInfo> bundles, string outputFolder)
	{
		var manifest = new Manifest();
		manifest.info = new VersionInfomation();
		manifest.info.BuildTime = DateTime.Now.ToString();
		//TODO:其余从配置读取

		//Todo:生成bundleList
		//todo:申城AssetList
		return manifest;
	}

	/// <summary>
	/// 生成BundleList
	/// </summary>
	public void GenerateBundleList(Manifest manifest, List<BuildBundleInfo> bundles, string outpoutFolder)
	{

	}
}
//怎么创建Manifest，以及怎么用fest
//对应依赖关系写完后，对应编辑器分包编写； 以及对应的资源管理器编写。对应的Provider进行编写。 初版
public class BuildSample
{
	[MenuItem("Tools/Build AssetBundles with Manifest")]
	public static void Build()
	{
		string outputFolder = Application.streamingAssetsPath + "/AssetBundles";
		//收集资源
		var bundles = CollectAndBuildBundles(outputFolder);

		//生成清单
		var builder = new ManifestBuilder();
		builder.Build(bundles, outputFolder);

		//保存清单，TODO：保存成Josn文件
	}

	private static List<ManifestBuilder.BuildBundleInfo> CollectAndBuildBundles(string outputFolder)
	{
		var bundles = new List<ManifestBuilder.BuildBundleInfo>();

		// 示例1: UI Bundle
		bundles.Add(new ManifestBuilder.BuildBundleInfo()
		{
			BundleName = "ui_elements.bundle",
			OutputFilePath = Path.Combine(outputFolder, "ui_elements.bundle"),
			Assets = new List<string>() {
				"Assets/Res/UI/Button.prefab",
				"Assets/Res/UI/Panel.prefab"
			}
		});

		// 示例2: 角色Bundle，依赖共享材质Bundle

		bundles.Add(new ManifestBuilder.BuildBundleInfo
		{
			BundleName = "characters.bundle",
			OutputFilePath = Path.Combine(outputFolder, "characters.bundle"),
			Assets = new List<string> { "Assets/Res/Characters/Hero.prefab" },
			Dependencies = new List<string> { "shared_materials.bundle" }, // 声明依赖
		});

		// 示例3: 共享材质Bundle
		bundles.Add(new ManifestBuilder.BuildBundleInfo
		{
			BundleName = "shared_materials.bundle",
			OutputFilePath = Path.Combine(outputFolder, "shared_materials.bundle"),
			Assets = new List<string> { "Assets/Res/Materials/Common.mat" },
		});
		return bundles;
	}
}
