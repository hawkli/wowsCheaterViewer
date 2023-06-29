# wowsCheaterViewer
战舰世界国服，wowsCheaterViewer封禁查看器插件（功能类似海猴查看器/Monitor）  
1、本插件目前仅支持国服  
2、首次安装后，需要设定游戏根路径，设定完成后插件会自动监控到对局开始  
3、本插件数据来源于国服官方接口与yuyuko机器人接口  
4、功能使用说明：  
  4.1、重设游戏路径：设定完游戏路径后，插件将自动监控对局并识别对局信息  
  4.2、刷新对局信息：重新识别当前正在进行中的对局信息，建议在读取失败的情况下使用  
  4.3、读取rep文件：识别rep文件中的对局信息。可以跨版本，但是无法读取到改名的玩家  
  4.4、标记（标记所有敌方）：根据uid标记玩家（可以直接在展示数据的标记列编辑，或点击“标记所有敌方”按钮进行批量标记），标记的内容将保存到本地配置文件  
  4.5、单个玩家调试：调试单个玩家需要从rep文件或对局文件或报错日志中获取。格式形如：  
       {"shipId":"", "relation":"", "id":"", "name":""}  
5、关于[封禁匹配]列的说明：  
  5.1、将鼠标移动到该列将显示玩家的历史封禁记录（包括曾用名、封禁时间、官方公布的id等）  
  5.2、封禁匹配的数据来源于yuyuko机器人接口，等效于wws ban  
  5.3、匹配的原理是检测yuyuko机器人收集过的玩家id是否与封禁名单匹配上  
  5.4、yuyuko机器人并未收集国服所有玩家信息，所以匹配数量即使为1也不能说明该玩家被封禁过  
6、插件会收集玩家对局数据提交给yuyuko机器人（包括玩家id、名称、军团等信息，不包括个人隐私数据）  
  
# 相关链接
*yuyuko封禁匹配：https://wows.mgaia.top/#/banLike  
*yuyuko机器人赞助地址：https://afdian.net/a/JustOneSummer  
*项目github地址：https://github.com/bbaoqaq/wowsCheaterViewer  
*项目gitee地址：https://gitee.com/bbaoqaq/wowsCheaterViewer  