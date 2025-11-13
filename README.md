# FontMaker

一个字库制作工具，是[Bitmap FontMaker项目](https://gitee.com/kerndev/FontMaker)的完整重写，使用C#和WPF实现，并添加了原工具中没有的功能。


## 如何为应用贡献语言

当前应用采用WPF推荐的多语言方案，所以添加语言需要重新编译，不支持动态添加。

首先安装插件ResXManager，Languages.resx中新建想添加的语言，并完成翻译

找到ViewModel中的LanguagesViewModel中的SupportedLanguageCodes，添加刚刚创建的语言代码即可

## 如何添加更多导出格式

导出的核心执行逻辑位于ViewModel中的ExportViewModel，首先添加对应的选择字段以及对应的逻辑函数，最终实现逻辑函数即可。

如果需要添加一个类来辅助导出，可以添加在Exporters中

如果是源代码类的导出，最好添加上详细的注释说明和基本的数据读取接口


## TODO

- [x] 添加本地化多语言
- [x] 添加字库编辑窗体
- [ ] 优化设置相关
- [ ] 完成更多导出格式
- [ ] 完成这个readme文档
- [ ] 优化切换语言后字符显示不完整的问题
- [ ] ...更多功能
