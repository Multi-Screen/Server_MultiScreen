using System.Collections;
using System.Collections.Generic;

public class Protocal
{

}

/// <summary>
/// 消息类型
/// </summary>
public enum MsgType
{
    Login,//登录消息
    HideCube
}
/// <summary>
/// 消息数据类
/// </summary>
public class MsgData
{
    /// <summary>
    /// 消息类型
    /// </summary>
    public MsgType msgType;
    /// <summary>
    /// 设备名称
    /// </summary>
    public string name;
    /// <summary>
    /// 消息内容
    /// </summary>
    public string msg;
}
