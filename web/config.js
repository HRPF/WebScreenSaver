window.appConfig = {
    clockType: "analog",           // 'digital' 或 'analog'
    showDate: true,                 // 是否显示日期
	timeFormat: "12h",              // "24h" 或 "12h"
	showSeconds: false,              // true 显示秒，false 隐藏秒
    color: "#404040",               // 数字时钟颜色 / 表盘边框颜色
	glowOpacity: 0.7,               // 辉光透明度（0-1），0 为无辉光
    backgroundColor: "#000000",     // 页面背景色
    fontFamily: "monospace, 'Courier New'", // 数字时钟字体
    fontSize: "8vw",                // 数字时钟字号
	dateColor: "#222222",           // 日期颜色
    dateFontSize: "2vw",            // 日期字体大小
    hourHandColor: "#666666",       // 时针颜色
    minuteHandColor: "#555555",     // 分针颜色
    secondHandColor: "#DD2222",     // 秒针颜色
    hourHandWidth: 4,               // 时针粗细（像素）
    minuteHandWidth: 2,             // 分针粗细
    secondHandWidth: 1,             // 秒针粗细
	analogSize: "50vmin",           // 控制模拟时钟容器的大小，可使用 vmin、px、% 等单位
	updateInterval: 1000            // 更新时间间隔（毫秒）
};