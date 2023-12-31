﻿# 项目介绍
`PluginHub`是一款用`IMGUI`编写、基于`EditorWindow`的`Unity3D`编辑器插件开发框架，并包含一些开箱即用的插件模块。您可以使用这些开发完成的插件模块提高您的工作效率。或者您也可以自己开发插件模块，以适应您自己的开发工作流。

每个模块实现一方面的功能。旨在为您提供各式各样的功能，以加速开发效率。感谢您点击右上角的`Star`以支持我的工作。

`PluginHub` is an `Unity3D` editor plugin development framework based on `EditorWindow` written in `IMGUI`, and contains some out-of-the-box plugin modules. You can use these developed plugin modules to improve your work efficiency. Or you can also develop plugin modules by yourself to adapt to your own development workflow.

Each module implements one aspect of the function. The purpose is to provide you with various functions to accelerate development efficiency. Thank you for clicking `Star` in the upper right corner to support my work.

`PluginHub`在`Unity3D`中以`PluginHub`窗口的形式呈现，您可以在窗口中查看和使用所有模块的功能，也可以在`PluginHub`配置文件中启用或禁用模块。

中文视频教程：https://www.bilibili.com/video/BV1H94y1a79d/

若您发现`PluginHub`中的任何问题，欢迎提交`Issue`或`Pull Request`。

## PluginHub主窗口
<img src="ReadmeImg/Demo0.png" width="400">

## 模块配置页面
<img src="ReadmeImg/Demo1.png" width="400">


# 名词

- `PluginHubWindow`：一个Unity3D编辑器窗口，所有插件模块`UI`都在这个窗口中呈现（见`PluginHubWindow`类）。打开`PluginHubWindow`的快捷键是`Ctrl+Alt+R`
- 插件模块：对应`PluginHubWindow`中的每个下拉卷展栏，实现一个方面的功能。也称作`Module`，基类为`PluginHubModuleBase`
- `ModuleConfigSO`:一个`ScriptableObject`配置文件，用于配置您需要启用的模块，启用的模块会在`PluginHubWindow`中显示。见`ModuleConfigSO`类
- `PH`:有时候您可能会在源码中看到`PH`这个缩写，它是`PluginHub`的缩写

# 特点

- 模块之间分类清晰，简单易用。可以通过每个模块的卷展栏按钮折叠和展开模块。
- 提供多种方便的模块功能，您也可以开发自己的模块，只需继承`PluginHubModuleBase`类即可。
- 可以通过`ScriptableObject`配置文件启用或禁用模块，以定制您干净整洁的`PluginHubWindow`。
- 包含完整源代码，您可以自由扩展和修改功能模块。


# 安装与使用

已经过测试的`Unity3D`版本：`2021.3.x` 以上。更老的版本可能也可以使用，但是未经测试，可能会有API不兼容的情况。

随着功能增加和框架变更，之前的旧模块可能会出现问题，目前还在积极开发中。任何使用问题请提交`Issue`。

1. 将存储库克隆到本地
2. 在`Unity3D`中打开`Package Manager`窗口
3. 点击`+`按钮，选择`Add package from disk...`
4. 选择`PluginHub`文件夹中的`package.json`文件
5. 等待`Unity3D`导入完成
6. 导航到`Window`->`PluginHub` 或者 `Ctrl+Alt+R`打开`PluginHubWindow`窗口
7. 在`PluginHubWindow`窗口中展开模块的下拉卷展栏
8. 开始使用
9. 如果想要定制您的`PluginHubWindow`，请导航到`PluginHub\Resources\PH_ModuleConfigSO.asset`，在检视面板中启用或禁用模块


# 已完成开发的模块
这里只介绍一些常用且成熟稳定的模块，更多模块请自行查看源码。

### NavigationBarModule

将常用的窗口，Unity文件夹，个人文件夹做成按钮便于访问。

<img src="ReadmeImg/NavigationBarModule.png" width="400">

### CommonComponentModule

将场景中经常使用的、重要的`GameObject`统一到一个窗口中，方便您随时选择他们。

也能根据场景相机的位置自动为您选择最近的`GameObject`。

<img src="ReadmeImg/CommonComponentModule.png" width="400">

### BuildModule

为用户提供一键打包功能，支持多平台。

构建项目：与`PlayerSettings`中的构建按钮功能相同。

构建当前场景：与构建项目类似，区别是`exe`执行文件和打包目录使用当前场景名称命名。

仅构建当前场景：程序会先在构建设置中取消勾选其他场景，只保留当前场景，以仅将当前场景打进包中，并且`exe`执行文件和打包目录使用当前场景名称命名。（也可指定其他名称）

<img src="ReadmeImg/BuildModule.png" width="400">

### SceneModule

可以查看项目中的所有场景资产，并可以进行过滤筛选。例如您可以输入`Main`来查找所有文件名中包含`Main`的场景。
方便用户快速定位到场景资产。


<img src="ReadmeImg/SceneModule.png" width="400">


### SelectionModule

功能围绕选中的`Object`展开。

例如：如果选中的是场景中的`Mesh`对象，模块会在场景视图中展示长宽高的数据。

<img src="ReadmeImg/SelectionModule.png">

### ShaderDebuggerModule

可以用该模块吸取场景中的颜色值，颜色值能够以`0-1`的浮点数和`0-255`的整数两种形式显示。便于Shader调试。


### ReferenceFinderModule

引用查找和替换。

一个使用场景是，您制作了一个新材质，想要将项目中所有使用旧材质的物体替换为新材质。

另一个使用场景，您想要查找项目中所有使用了某个`Shader`的物体。或者您想要知道自己是否可以安全的删除某个`Shader`。

### AlignModule

想要将场景中的物体对齐到某个物体上？这个模块可以帮助您。

例如将灯模型对齐天花板，将桌子模型对齐到地面。

也可以以指定的距离和方向移动物体。


<img src="ReadmeImg/AlignModule.png" width="400">

### TextureProcessModule

在URP或HDRP中，通常材质要求使用一张纹理的多个通道来提供不同的信息。例如：HDRP中金属度和光滑度使用同一张纹理的不同通道来提供。

这时候您可以使用该模块将两张纹理中指定的通道合并为一张纹理。

又例如您下载到的是Roughness纹理，但是Unity通常需要的是Smoothness纹理，这时候您可以使用该模块将Roughness纹理转换为Smoothness纹理。（Roughness=1-Smoothness）

<img src="ReadmeImg/TextureProcessingModule.png" width="400">

### LightProbePlacementModule

程序化控制LightProbe的放置，节约您的时间。

<img src="ReadmeImg/LightProbePlacementModule.png" width="400">

### MaterialOptimizationModule

快速提取嵌入式材质

以名称相同、主纹理相同、名称相似的条件搜索场景中类似的材质，为您减少DrawCall的后续操作提供参考。

将场景中所有对旧材质的引用替换为新材质。

<img src="ReadmeImg/MaterialOptimizationModule.png" width="400">

### MaterialReplaceModule

使用给定材质替换场景中所有材质。不会破坏场景，可以随时还原。

### MeshToHeightModule

模块会用竖直向下投射射线的方式，将任意`Mesh`转换为`HeightMap`，可用于生成地形图等场景。


### EditorExtensionModule

编辑器扩展模块，一个特殊模块。为`Unity`编辑器提供额外菜单项，以增强编辑器功能。启用后，顶部菜单会出现`PluginHub`菜单项。

<img src="ReadmeImg/EditorExtensionModule.png" width="400">
<br>
<br>
<img src="ReadmeImg/EditorExtensionModule2.png" width="400">