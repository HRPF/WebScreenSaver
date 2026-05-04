// clock.js - 简约时钟屏保核心逻辑
// 配置通过 window.appConfig 从 config.js 注入

// ================== 默认配置 ==================
const DEFAULT_CONFIG = {
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

// 合并配置：优先使用 window.appConfig（由 config.js 提供）
let config = { ...DEFAULT_CONFIG, ...(window.appConfig || {}) };

// 定时器和动画句柄
let analogInterval = null;
let dateInterval = null;
let animationFrame = null;

// ================== 预览模式支持 ==================
// 检测是否在预览模式中（通过 URL 参数 preview=true）
const urlParams = new URLSearchParams(window.location.search);
const isPreviewMode = urlParams.has('preview');

/**
 * 动态更新配置并重新初始化时钟
 * @param {Object} newConfig 新的配置对象
 */
function updateConfig(newConfig) {
    // 合并新配置到当前配置
    config = { ...config, ...newConfig };
    console.log('配置已更新:', config);

    // 应用静态样式（背景、字体等）
    applyStaticStyles();

    // 重新初始化时钟
    initClock();
}

// 监听来自父窗口的消息（用于配置更新）
window.addEventListener('message', function(event) {
    // 安全性检查：仅接受来自同源的消息，但预览模式需要允许所有源
    // 在生产环境中可以考虑检查 event.origin
    if (event.data && event.data.type === 'updateConfig') {
        console.log('收到配置更新:', event.data.config);
        updateConfig(event.data.config);
    }
});

// 如果是在预览模式中，通知父窗口已就绪
if (isPreviewMode) {
    // 延迟发送，确保 DOM 已加载
    setTimeout(() => {
        if (window.parent && window.parent !== window) {
            window.parent.postMessage({
                type: 'previewReady',
                message: '预览时钟已加载'
            }, '*');
        }
    }, 100);
}

// ================== 辅助函数 ==================
/**
 * 将十六进制颜色字符串（如 #FFFFFF）转换为 RGB 对象
 * @param {string} hex 十六进制颜色，支持 #RGB 或 #RRGGBB
 * @returns {{r: number, g: number, b: number} | null}
 */
function hexToRgb(hex) {
    // 移除 # 号
    hex = hex.replace(/^#/, '');
    // 处理简写 #RGB
    if (hex.length === 3) {
        hex = hex.split('').map(c => c + c).join('');
    }
    if (hex.length === 6) {
        const r = parseInt(hex.substring(0, 2), 16);
        const g = parseInt(hex.substring(2, 4), 16);
        const b = parseInt(hex.substring(4, 6), 16);
        return { r, g, b };
    }
    return null; // 非十六进制格式
}

// ================== 核心函数 ==================

/**
 * 应用静态样式（背景、字体）
 */
function applyStaticStyles() {
    document.body.style.backgroundColor = config.backgroundColor;
    const digitalClock = document.getElementById('digital-clock');
    if (digitalClock) {
        digitalClock.style.fontFamily = config.fontFamily;
        digitalClock.style.fontSize = config.fontSize;
    }
}

/**
 * 更新数字时钟显示（支持 12h/24h 格式，并动态设置辉光）
 */
function updateDigitalClock() {
    const now = new Date();
    let hours = now.getHours();
    const minutes = now.getMinutes().toString().padStart(2, '0');
    const seconds = now.getSeconds().toString().padStart(2, '0');
    let ampm = '';

    if (config.timeFormat === '12h') {
        ampm = hours >= 12 ? 'PM' : 'AM';
        hours = hours % 12;
        if (hours === 0) hours = 12;
    }

    const hourStr = hours.toString().padStart(2, '0');
    let timeStr;
	if (config.showSeconds) {
		const seconds = now.getSeconds().toString().padStart(2, '0');
		timeStr = `${hourStr}:${minutes}:${seconds}`;
	} else {
		timeStr = `${hourStr}:${minutes}`;
	}
    const digitalElem = document.getElementById('digital-clock');

    // 清空容器，根据格式重新填充
    digitalElem.innerHTML = '';

    if (config.timeFormat === '12h') {
        const ampmSpan = document.createElement('span');
        ampmSpan.className = 'ampm';
        ampmSpan.textContent = ampm;
        digitalElem.appendChild(ampmSpan);

        const timeSpan = document.createElement('span');
        timeSpan.className = 'time';
        timeSpan.textContent = timeStr;
        digitalElem.appendChild(timeSpan);

        digitalElem.classList.add('format-12h');
    } else {
        digitalElem.textContent = timeStr;
        digitalElem.classList.remove('format-12h');
    }

    // 设置颜色
    digitalElem.style.color = config.color;

    // ===== 设置辉光（text-shadow）颜色与数字颜色一致，透明度可调 =====
    const rgb = hexToRgb(config.color);
    if (rgb) {
        // 十六进制颜色，使用 rgba 动态设置辉光
        digitalElem.style.textShadow = `0 0 20px rgba(${rgb.r}, ${rgb.g}, ${rgb.b}, ${config.glowOpacity})`;
    } else {
        // 如果不是十六进制，回退到使用颜色字符串直接设置（不支持透明度调整）
        // 或者使用默认白色辉光（透明度保持）
        digitalElem.style.textShadow = `0 0 20px ${config.color}`;
        // 为了支持透明度，可以尝试解析其他格式，但这里简化处理
    }
}

/**
 * 设置模拟时钟指针角度及样式（保持不变）
 */
function setHandAngles(hourDeg, minuteDeg, secondDeg) {
    const hourHand = document.querySelector('.hour-hand');
    const minuteHand = document.querySelector('.minute-hand');
    const secondHand = document.querySelector('.second-hand');
    const clockFace = document.querySelector('.clock-face');

    if (!hourHand || !minuteHand || !secondHand || !clockFace) return;

    hourHand.style.transform = `translateX(-50%) rotate(${hourDeg}deg)`;
    minuteHand.style.transform = `translateX(-50%) rotate(${minuteDeg}deg)`;
    secondHand.style.transform = `translateX(-50%) rotate(${secondDeg}deg)`;

    hourHand.style.backgroundColor = config.hourHandColor;
    minuteHand.style.backgroundColor = config.minuteHandColor;
    secondHand.style.backgroundColor = config.secondHandColor;

    hourHand.style.width = config.hourHandWidth + 'px';
    minuteHand.style.width = config.minuteHandWidth + 'px';
    secondHand.style.width = config.secondHandWidth + 'px';

    clockFace.style.borderColor = config.color;
}

/**
 * 获取当前时间对应的指针角度
 */
function getCurrentAngles() {
    const now = new Date();
    const hours = now.getHours() % 12;
    const minutes = now.getMinutes();
    const seconds = now.getSeconds();

    return {
        hour: hours * 30 + minutes * 0.5,
        minute: minutes * 6 + seconds * 0.1,
        second: seconds * 6
    };
}

/**
 * 更新模拟时钟到当前时间（无动画）
 */
function updateAnalogClock() {
    const angles = getCurrentAngles();
    setHandAngles(angles.hour, angles.minute, angles.second);
}

/**
 * 启动模拟时钟动画：指针从12点方向快速旋转到当前时间
 */
function startAnalogClockWithAnimation() {
    setHandAngles(0, 0, 0);
    const target = getCurrentAngles();
    const duration = 800;
    const startTime = performance.now();
    const easeOutCubic = t => 1 - Math.pow(1 - t, 3);

    const animate = (currentTime) => {
        const elapsed = currentTime - startTime;
        const progress = Math.min(elapsed / duration, 1);
        const eased = easeOutCubic(progress);

        setHandAngles(
            target.hour * eased,
            target.minute * eased,
            target.second * eased
        );

        if (progress < 1) {
            animationFrame = requestAnimationFrame(animate);
        } else {
            setHandAngles(target.hour, target.minute, target.second);
            startAnalogClockUpdates();
        }
    };

    animationFrame = requestAnimationFrame(animate);
}

/**
 * 启动模拟时钟的周期性更新（正常走时）
 */
function startAnalogClockUpdates() {
    updateAnalogClock();
    analogInterval = setInterval(updateAnalogClock, config.updateInterval);
}

/**
 * 更新日期显示（自定义格式，星期前加两个空格）
 */
function updateDate() {
    const now = new Date();
    const year = now.getFullYear();
    const month = now.getMonth() + 1;
    const day = now.getDate();
    const weekdays = ['星期日', '星期一', '星期二', '星期三', '星期四', '星期五', '星期六'];
    const weekday = weekdays[now.getDay()];

    const dateString = `${year}年${month}月${day}日  ${weekday}`;
    const dateElem = document.getElementById('date-display');
    if (dateElem) {
        dateElem.textContent = dateString;
        dateElem.style.color = config.dateColor;
        dateElem.style.fontSize = config.dateFontSize;
    }
}

/**
 * 初始化时钟：根据配置显示数字/模拟时钟，并启动更新
 */
function initClock() {
    // 清除之前的定时器和动画
    if (analogInterval) clearInterval(analogInterval);
    if (dateInterval) clearInterval(dateInterval);
    if (animationFrame) cancelAnimationFrame(animationFrame);

    applyStaticStyles();

    const digitalElem = document.querySelector('.clock');
    const analogElem = document.querySelector('.analog-container');
    const dateElem = document.getElementById('date-display');

    if (!digitalElem || !analogElem || !dateElem) {
        console.error('时钟元素未找到，请检查 HTML 结构');
        return;
    }

    if (config.clockType === 'analog') {
        digitalElem.style.display = 'none';
        analogElem.style.display = 'block';
		analogElem.style.width = config.analogSize;
		analogElem.style.height = config.analogSize;   // 假设宽高相等，保持正方形
        // 预览模式下跳过初始动画，立即显示正确时间并开始更新
        if (isPreviewMode) {
            startAnalogClockUpdates();
        } else {
            startAnalogClockWithAnimation();
        }
    } else {
        digitalElem.style.display = 'block';
        analogElem.style.display = 'none';
        updateDigitalClock();
        analogInterval = setInterval(updateDigitalClock, config.updateInterval);
    }

    if (config.showDate) {
        dateElem.style.display = 'block';
        dateElem.style.color = config.dateColor;
        dateElem.style.fontSize = config.dateFontSize;
        updateDate();
        dateInterval = setInterval(updateDate, 60000);
    } else {
        dateElem.style.display = 'none';
    }
}

// ================== 启动时钟（等待 DOM 加载） ==================
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initClock);
} else {
    initClock();
}