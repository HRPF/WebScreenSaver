// 时钟配置
let config = {
    clockType: 'digital',  // 'digital' 或 'analog'
    showDate: true,
    color: '#FFFFFF',
    updateInterval: 1000
};

// 尝试加载配置文件
try {
    fetch('config.json')
        .then(response => response.json())
        .then(data => {
            config = { ...config, ...data };
            initClock();
        })
        .catch(() => initClock());
} catch {
    initClock();
}

function initClock() {
    // 根据配置显示相应时钟
    if (config.clockType === 'analog') {
        document.querySelector('.clock').style.display = 'none';
        document.querySelector('.analog-container').style.display = 'block';
        updateAnalogClock();
        setInterval(updateAnalogClock, config.updateInterval);
    } else {
        document.querySelector('.clock').style.display = 'block';
        document.querySelector('.analog-container').style.display = 'none';
        updateDigitalClock();
        setInterval(updateDigitalClock, config.updateInterval);
    }

    // 更新日期
    if (config.showDate) {
        updateDate();
        setInterval(updateDate, 60000); // 每分钟更新一次日期
    } else {
        document.getElementById('date-display').style.display = 'none';
    }
}

// 数字时钟
function updateDigitalClock() {
    const now = new Date();
    const hours = now.getHours().toString().padStart(2, '0');
    const minutes = now.getMinutes().toString().padStart(2, '0');
    const seconds = now.getSeconds().toString().padStart(2, '0');

    document.getElementById('digital-clock').textContent =
        `${hours}:${minutes}:${seconds}`;

    // 应用颜色
    document.querySelector('.clock').style.color = config.color;
}

// 模拟时钟
function updateAnalogClock() {
    const now = new Date();
    const hours = now.getHours() % 12;
    const minutes = now.getMinutes();
    const seconds = now.getSeconds();

    // 计算角度
    const hourDeg = (hours * 30) + (minutes * 0.5);
    const minuteDeg = (minutes * 6) + (seconds * 0.1);
    const secondDeg = seconds * 6;

    // 更新指针
    const hourHand = document.querySelector('.hour-hand');
    const minuteHand = document.querySelector('.minute-hand');
    const secondHand = document.querySelector('.second-hand');

    hourHand.style.transform = `translateX(-50%) rotate(${hourDeg}deg)`;
    minuteHand.style.transform = `translateX(-50%) rotate(${minuteDeg}deg)`;
    secondHand.style.transform = `translateX(-50%) rotate(${secondDeg}deg)`;

    // 应用颜色
    hourHand.style.backgroundColor = config.color;
    minuteHand.style.backgroundColor = config.color;
    document.querySelector('.clock-face').style.borderColor = config.color;
}

// 更新日期
function updateDate() {
    const now = new Date();
    const options = { year: 'numeric', month: 'long', day: 'numeric', weekday: 'long' };
    const dateString = now.toLocaleDateString('zh-CN', options);
    document.getElementById('date-display').textContent = dateString;
}