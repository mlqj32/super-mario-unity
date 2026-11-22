using System.Collections.Generic;

namespace Mario.Syobon
{
    // Localized presentation replaces placeholder messages in the XNA port.
    public static class SyobonText
    {
        static readonly string[] messages={"",
            "通关成功!","感谢游玩!","好吃!","没有毒 但是...","被刺中了!","真不该吃!","燃烧起来了!",
            "身体在燃烧...","烟花!","完蛋了!","我的脚!","八百度!","和岩浆合体了...",
            "呀呼!","我赢了?","到此为止!","再见了!","我最强!","下次再来!","绝不后退!","哈哈!",
            "呀呼!","我赢了?","到此为止!","不自量力!","大意了吧!","真有意思!","太弱了!","太冒险了!",
            "呀呼!","我赢了?","再见了!","不自量力!","我不会输!","看穿你的行动!","粉碎吧!","任务完成!",
            "铁壁 无敌!","自作自受!","ZZZ","熊来了!","?","真不该吃!","好吃!","小看砖块了吧?","波动炮!","以为我背叛了?",
            "制作与测试","第一关测试","原版测试者","第二关测试","WILLOWLET","第三关测试","WILLOWLET","第四关测试","ANN",
            "感谢协助","T前辈","S前辈","视频技术","K前辈","录制与剪辑","WILLOWLET","程序 美术 创意","CHIKU","感谢游玩猫里奥!",
            "提示","欢迎来到猫里奥!","方向键或摇杆移动","Z / 空格 / 手柄A跳跃","菜单中可以选择关卡","按确认继续",
            "小心脚下!","按确认继续","不要相信眼前的一切!","按确认继续","前方可能有陷阱","请仔细观察",
            "温馨提示","看起来安全的地方","也可能有危险","按确认继续","没有什么是绝对安全的","试试不同的路线","按确认继续",
            "最后的提示","坚持就能找到答案","死亡后可立即重试","暂停菜单可以重玩本关","按确认继续"};
        public static string Translate(string text)
        {
            if(string.IsNullOrEmpty(text))return "";
            if(text.StartsWith("Message ")&&int.TryParse(text.Substring(8),out int i))
                return i>0&&i<messages.Length?messages[i]:"按确认继续";
            if(text=="Prece Enter Key ")return "按确认开始";
            if(text=="WELCOME TO OWATA ZONE")return "欢迎来到陷阱区";
            if(text.IndexOf('ƒ')>=0)return "感谢游玩!";
            return text;
        }
    }
}
