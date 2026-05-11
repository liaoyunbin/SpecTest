//UTF-8格式
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class VersionInfomation
{
	public string PackageVersion = "v1.0.0";			//资源包版本号。标记资源变化，用对热更对比
	public string BuildVersion = "2024051101";			//构建版本号：点击build时会重新生成，主要面向程序用于排查资源相关的问题，主要用时间戳
	public string BuildTime = "2024-05-11 10:30:00";	//构建时间
	public string PackageName = "DefaultPackage";		//包名称
}

/// <summary>
/// 资源列表
/// </summary>
public class AssetList
{
	public string Address; //资源地址，用于代码加载. 通过对应的Rule规则进行分类
	public string AssetPath; //资源在项目中的路径
	public string AssetGuid; //资源的Guid
	public Type AssetType; //资源类型

	public int BundleId; //所属的BundleID
	public List<int> DependIds; //依赖的BundleId
	public List<string> Tags;//资源标签 ？？ 不太确定这个需不需要
}

/// <summary>
/// AssetBundle的信息
/// </summary>
public class BunldeList
{
	public int BunldeId; //Bundle的唯一ID
	public string BunldeName; //Bundle名称:shared_common_textures.bundle
	public string FileName;// 实际文件名.资源包在磁盘或服务器上存储时的真实文件名:shared_common_textures_3a8f5c.bundle（3a8f5c是哈希值片段）

	//哈希值用于“认人”（版本管理、内容标识），是热更新的决策依据。
	//校验码用于“验伤”（数据传输完整性检查），是确保加载过程可靠的辅助手段。
	//在YooAsset的体系中，哈希值扮演了更核心的角色
	public string FileHash; //文件哈希值，用于检测文件是否变更.数字指纹
	public string FileCRC; //循环冗余验证，用于确保文件完整性
	public long FileSize; //文件大小
	public bool Flags; //包含压缩方式，是否加密等

	public List<int> DependIds; //依赖的BundleId
	public List<string> Tags;//Bundle标签 ？？ 不太确定这个需不需要
}

//将代码中的资源地址（如 "ui_main_menu"）映射到实际的 AssetBundle 文件
//记录资源之间的依赖关系，确保加载顺序正确
//版本控制:通过哈希值和 CRC 校验确保资源版本一致性。
//增量更新基础:比较新旧 Manifest 的哈希值，确定需要更新的文件
//标签分类管理:通过 AssetTags 和 Bundle Tags 实现按需加载和卸载
public class Manifest
{
	public VersionInfomation info;
	public List<AssetList> assets;
	public List<BunldeList> bundles;
}

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
		public List <int> DependIds;
	}
	public class BuildBundleInfo
	{
		public string BundleName;
		public string OutputFilePath;
		public List <string> Dependencies;
		public List<string> Assets;//包含的资源路径？？？	
	}
	//TODO:序列化成三份json文件
	public Manifest Build(List<BuildBundleInfo> bundles, string outputFolder)
	{
		var manifest = new Manifest();
		return manifest;
	}

	/// <summary>
	/// 生成BundleList
	/// </summary>
	public void GenerateBundleList(Manifest manifest, List<BuildBundleInfo> bundles,string outpoutFolder)
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
			BundleName ="ui_elements.bundle",
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
