// config-ui.js - 配置页面逻辑

// 默认配置（与 clock.js 中的 DEFAULT_CONFIG 保持一致）
const DEFAULT_CONFIG = {
    clockType: "analog",
    showDate: true,
    timeFormat: "12h",
    showSeconds: false,
    color: "#404040",
    glowOpacity: 0.7,
    backgroundColor: "#000000",
    fontFamily: "monospace, 'Courier New'",
    fontSize: "8vw",
    dateColor: "#222222",
    dateFontSize: "2vw",
    hourHandColor: "#666666",
    minuteHandColor: "#555555",
    secondHandColor: "#DD2222",
    hourHandWidth: 4,
    minuteHandWidth: 2,
    secondHandWidth: 1,
    analogSize: "50vmin",
    updateInterval: 1000
};

// 当前配置（从 config.js 加载或使用默认值）
let currentConfig = { ...DEFAULT_CONFIG };

// 预览 iframe 引用
let previewFrame = null;

// 防抖计时器
let previewUpdateTimer = null;

// DOM 加载完成后初始化
document.addEventListener('DOMContentLoaded', function() {
    previewFrame = document.getElementById('preview-frame');

    // 加载当前配置到表单
    loadCurrentConfig();

    // 设置事件监听器
    setupEventListeners();

    // 初始预览更新
    updatePreview();
});

/**
 * 加载当前配置到表单
 */
function loadCurrentConfig() {
    try {
        // 如果 window.appConfig 存在（来自 config.js），则使用它
        if (window.appConfig && typeof window.appConfig === 'object') {
            currentConfig = { ...DEFAULT_CONFIG, ...window.appConfig };
            console.log('已加载当前配置');
        } else {
            console.warn('未找到当前配置，使用默认设置');
            currentConfig = { ...DEFAULT_CONFIG };
        }

        // 更新表单控件
        updateFormFromConfig();

    } catch (error) {
        console.error('加载配置时出错:', error);
        alert('无法加载当前配置，使用默认设置');
        currentConfig = { ...DEFAULT_CONFIG };
        updateFormFromConfig();
    }
}

/**
 * 根据当前配置更新表单控件
 */
function updateFormFromConfig() {
    // 时钟类型
    document.querySelector(`input[name="clockType"][value="${currentConfig.clockType}"]`).checked = true;

    // 时间格式
    document.querySelector(`input[name="timeFormat"][value="${currentConfig.timeFormat}"]`).checked = true;

    // 复选框
    document.getElementById('showSeconds').checked = currentConfig.showSeconds;
    document.getElementById('showDate').checked = currentConfig.showDate;

    // 颜色
    document.getElementById('color').value = currentConfig.color;
    document.getElementById('backgroundColor').value = currentConfig.backgroundColor;
    document.getElementById('dateColor').value = currentConfig.dateColor;
    document.getElementById('glowOpacity').value = currentConfig.glowOpacity;
    document.getElementById('glowOpacity-value').textContent = currentConfig.glowOpacity;

    // 字体和尺寸
    document.getElementById('fontFamily').value = currentConfig.fontFamily;
    document.getElementById('fontSize').value = currentConfig.fontSize;
    document.getElementById('dateFontSize').value = currentConfig.dateFontSize;

    // 模拟时钟设置
    document.getElementById('analogSize').value = currentConfig.analogSize;
    document.getElementById('hourHandColor').value = currentConfig.hourHandColor;
    document.getElementById('minuteHandColor').value = currentConfig.minuteHandColor;
    document.getElementById('secondHandColor').value = currentConfig.secondHandColor;
    document.getElementById('hourHandWidth').value = currentConfig.hourHandWidth;
    document.getElementById('minuteHandWidth').value = currentConfig.minuteHandWidth;
    document.getElementById('secondHandWidth').value = currentConfig.secondHandWidth;

    // 高级设置
    document.getElementById('updateInterval').value = currentConfig.updateInterval;
}

/**
 * 从表单更新当前配置
 */
function updateConfigFromForm() {
    currentConfig = {
        clockType: document.querySelector('input[name="clockType"]:checked').value,
        timeFormat: document.querySelector('input[name="timeFormat"]:checked').value,
        showSeconds: document.getElementById('showSeconds').checked,
        showDate: document.getElementById('showDate').checked,
        color: document.getElementById('color').value,
        backgroundColor: document.getElementById('backgroundColor').value,
        dateColor: document.getElementById('dateColor').value,
        glowOpacity: parseFloat(document.getElementById('glowOpacity').value),
        fontFamily: document.getElementById('fontFamily').value,
        fontSize: document.getElementById('fontSize').value,
        dateFontSize: document.getElementById('dateFontSize').value,
        analogSize: document.getElementById('analogSize').value,
        hourHandColor: document.getElementById('hourHandColor').value,
        minuteHandColor: document.getElementById('minuteHandColor').value,
        secondHandColor: document.getElementById('secondHandColor').value,
        hourHandWidth: parseInt(document.getElementById('hourHandWidth').value, 10),
        minuteHandWidth: parseInt(document.getElementById('minuteHandWidth').value, 10),
        secondHandWidth: parseInt(document.getElementById('secondHandWidth').value, 10),
        updateInterval: parseInt(document.getElementById('updateInterval').value, 10)
    };
}

/**
 * 设置事件监听器
 */
function setupEventListeners() {
    // 表单控件变化时更新预览（防抖）
    const formControls = document.querySelectorAll('#config-form input, #config-form select');
    formControls.forEach(control => {
        control.addEventListener('change', handleFormChange);
        if (control.type === 'range') {
            control.addEventListener('input', handleFormChange);
        }
    });

    // 范围滑块数值显示
    const glowOpacitySlider = document.getElementById('glowOpacity');
    glowOpacitySlider.addEventListener('input', function() {
        document.getElementById('glowOpacity-value').textContent = this.value;
    });

    // 重置按钮
    document.getElementById('reset-btn').addEventListener('click', resetToDefaults);

    // 保存按钮
    document.getElementById('save-btn').addEventListener('click', saveConfig);

    // 复制按钮
    document.getElementById('copy-btn').addEventListener('click', copyToClipboard);

    // 关闭输出按钮
    document.getElementById('close-output-btn').addEventListener('click', function() {
        document.getElementById('output-section').style.display = 'none';
    });

    // 预览 iframe 加载完成
    previewFrame.addEventListener('load', function() {
        console.log('预览 iframe 加载完成');
        updatePreview();
    });
}

/**
 * 处理表单变化事件（防抖）
 */
function handleFormChange() {
    // 更新当前配置
    updateConfigFromForm();

    // 防抖预览更新（300ms）
    if (previewUpdateTimer) {
        clearTimeout(previewUpdateTimer);
    }
    previewUpdateTimer = setTimeout(() => {
        updatePreview();
        previewUpdateTimer = null;
    }, 300);
}

/**
 * 更新预览 iframe
 */
function updatePreview() {
    if (!previewFrame || !previewFrame.contentWindow) {
        console.warn('预览 iframe 未就绪');
        return;
    }

    try {
        // 发送配置到预览 iframe
        previewFrame.contentWindow.postMessage({
            type: 'updateConfig',
            config: currentConfig
        }, '*');

        console.log('预览已更新');
    } catch (error) {
        console.error('更新预览时出错:', error);
    }
}

/**
 * 重置为默认设置
 */
function resetToDefaults() {
    if (confirm('确定要恢复默认设置吗？当前更改将丢失。')) {
        currentConfig = { ...DEFAULT_CONFIG };
        updateFormFromConfig();
        updatePreview();
    }
}

/**
 * 保存配置（生成 config.js 内容）
 */
function saveConfig() {
    // 验证配置
    if (!validateConfig()) {
        alert('请检查设置，部分值无效。');
        return;
    }

    // 生成 config.js 内容
    const configContent = generateConfigJS();

    // 显示输出区域
    const outputSection = document.getElementById('output-section');
    const outputTextarea = document.getElementById('config-output');

    outputTextarea.value = configContent;
    outputSection.style.display = 'block';

    // 滚动到输出区域
    outputSection.scrollIntoView({ behavior: 'smooth' });
}

/**
 * 验证配置值
 */
function validateConfig() {
    const config = currentConfig;

    // 检查数值范围
    if (config.glowOpacity < 0 || config.glowOpacity > 1) {
        alert('辉光透明度必须在 0 到 1 之间');
        return false;
    }

    if (config.updateInterval < 100 || config.updateInterval > 5000) {
        alert('更新时间间隔必须在 100 到 5000 毫秒之间');
        return false;
    }

    if (config.hourHandWidth < 1 || config.hourHandWidth > 10 ||
        config.minuteHandWidth < 1 || config.minuteHandWidth > 10 ||
        config.secondHandWidth < 1 || config.secondHandWidth > 10) {
        alert('指针粗细必须在 1 到 10 像素之间');
        return false;
    }

    // 检查颜色格式（简单验证）
    const colorRegex = /^#[0-9A-F]{6}$/i;
    if (!colorRegex.test(config.color)) {
        alert('时钟颜色格式无效，请使用 #RRGGBB 格式');
        return false;
    }

    if (!colorRegex.test(config.backgroundColor)) {
        alert('背景颜色格式无效，请使用 #RRGGBB 格式');
        return false;
    }

    return true;
}

/**
 * 生成 config.js 内容
 */
function generateConfigJS() {
    const config = currentConfig;

    return `window.appConfig = {
    clockType: "${config.clockType}",           // 'digital' 或 'analog'
    showDate: ${config.showDate},                 // 是否显示日期
    timeFormat: "${config.timeFormat}",              // "24h" 或 "12h"
    showSeconds: ${config.showSeconds},              // true 显示秒，false 隐藏秒
    color: "${config.color}",               // 数字时钟颜色 / 表盘边框颜色
    glowOpacity: ${config.glowOpacity},               // 辉光透明度（0-1），0 为无辉光
    backgroundColor: "${config.backgroundColor}",     // 页面背景色
    fontFamily: "${config.fontFamily.replace(/"/g, '\\"')}", // 数字时钟字体
    fontSize: "${config.fontSize}",                // 数字时钟字号
    dateColor: "${config.dateColor}",           // 日期颜色
    dateFontSize: "${config.dateFontSize}",            // 日期字体大小
    hourHandColor: "${config.hourHandColor}",       // 时针颜色
    minuteHandColor: "${config.minuteHandColor}",     // 分针颜色
    secondHandColor: "${config.secondHandColor}",     // 秒针颜色
    hourHandWidth: ${config.hourHandWidth},               // 时针粗细（像素）
    minuteHandWidth: ${config.minuteHandWidth},             // 分针粗细
    secondHandWidth: ${config.secondHandWidth},             // 秒针粗细
    analogSize: "${config.analogSize}",           // 控制模拟时钟容器的大小，可使用 vmin、px、% 等单位
    updateInterval: ${config.updateInterval}            // 更新时间间隔（毫秒）
};`;
}

/**
 * 复制到剪贴板
 */
function copyToClipboard() {
    const textarea = document.getElementById('config-output');

    try {
        textarea.select();
        textarea.setSelectionRange(0, 99999); // 移动设备支持

        const successful = document.execCommand('copy');
        if (successful) {
            alert('配置已复制到剪贴板！');
        } else {
            alert('复制失败，请手动选择并复制文本。');
        }
    } catch (error) {
        console.error('复制失败:', error);
        alert('复制失败，请手动选择并复制文本。');
    }
}

// 监听来自预览 iframe 的消息（用于调试）
window.addEventListener('message', function(event) {
    if (event.data && event.data.type === 'previewReady') {
        console.log('预览 iframe 已就绪');
        updatePreview();
    }
});