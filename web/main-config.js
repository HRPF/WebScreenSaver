(function () {
    'use strict';

    var screensavers = [];
    var currentSelection = null;
    var pendingSelection = null;
    var savedSettings = null;

    var listEl = document.getElementById('screensaver-list');
    var frame = document.getElementById('preview-frame');
    var placeholder = document.getElementById('preview-placeholder');
    var btnConfigure = document.getElementById('btn-configure');
    var btnApply = document.getElementById('btn-apply');
    var actionInfo = document.getElementById('action-info');
    var countBadge = document.getElementById('count-badge');

    // ── Check if running inside WebView2 ──
    var isWebView = typeof chrome !== 'undefined' && chrome.webview;

    if (!isWebView) {
        listEl.innerHTML = '<p style="color:#666;padding:12px">请在屏保配置窗口中打开</p>';
        return;
    }

    // ── Helper: send JSON message to C# host ──
    function sendToHost(obj) {
        chrome.webview.postMessage(JSON.stringify(obj));
    }

    // ── Forward saved settings to the preview iframe ──
    function forwardSettingsToPreview() {
        if (savedSettings && frame.contentWindow) {
            frame.contentWindow.postMessage({
                type: 'updateConfig',
                config: savedSettings
            }, '*');
        }
    }

    // ── Show info text in the action bar ──
    function setActionInfo(text, isHint) {
        actionInfo.textContent = text || '';
        actionInfo.className = 'action-info' + (isHint ? ' hint' : '');
    }

    // ── Listen for messages from C# host ──
    chrome.webview.addEventListener('message', function (e) {
        var msg = e.data;
        if (!msg) return;

        switch (msg.type) {
            case 'init':
                screensavers = msg.screensavers || [];
                currentSelection = msg.currentSelection || null;
                pendingSelection = currentSelection;
                savedSettings = msg.settings || null;
                renderList();
                if (currentSelection) {
                    selectScreensaver(currentSelection);
                }
                break;
            case 'loadSettings':
                savedSettings = msg.settings || null;
                forwardSettingsToPreview();
                break;
            case 'configNotFound':
                setActionInfo('这个屏幕保护程序没有可以设置的选项', true);
                setTimeout(function () { setActionInfo(''); }, 3000);
                break;
        }
    });

    // ── Listen for previewReady from iframe ──
    window.addEventListener('message', function (e) {
        if (e.data && e.data.type === 'previewReady') {
            forwardSettingsToPreview();
        }
    });

    // ── Get icon for a screensaver (based on name) ──
    function getIcon(id) {
        var icons = ['&#9670;', '&#9733;', '&#9829;', '&#9729;', '&#9788;', '&#9730;', '&#9881;', '&#9835;'];
        var hash = 0;
        for (var i = 0; i < id.length; i++) { hash = id.charCodeAt(i) + ((hash << 5) - hash); }
        return icons[Math.abs(hash) % icons.length];
    }

    // ── Render screensaver list ──
    function renderList() {
        listEl.innerHTML = '';
        countBadge.textContent = screensavers.length;
        screensavers.forEach(function (id) {
            var card = document.createElement('div');
            card.className = 'screensaver-card' + (id === currentSelection ? ' active' : '');
            card.dataset.id = id;

            var top = document.createElement('div');
            top.className = 'card-top';

            var icon = document.createElement('span');
            icon.className = 'card-icon';
            icon.innerHTML = getIcon(id);
            top.appendChild(icon);

            var nameEl = document.createElement('div');
            nameEl.className = 'name';
            nameEl.textContent = id;
            top.appendChild(nameEl);

            card.appendChild(top);

            var folderEl = document.createElement('div');
            folderEl.className = 'folder';
            folderEl.textContent = 'web/' + id + '/';
            card.appendChild(folderEl);

            card.addEventListener('click', function () {
                selectScreensaver(id);
            });

            listEl.appendChild(card);
        });
    }

    // ── Select a screensaver ──
    function selectScreensaver(id) {
        pendingSelection = id;
        savedSettings = null;
        setActionInfo('点击"样式设置"可修改外观，点击"启用屏保"将其设为屏幕保护程序');

        // Update card highlight
        var cards = listEl.querySelectorAll('.screensaver-card');
        cards.forEach(function (c) {
            c.classList.toggle('active', c.dataset.id === id);
        });

        // Show preview
        placeholder.classList.add('hidden');
        frame.src = 'https://screensaver.local/' + encodeURIComponent(id) + '/index.html?preview=true';
        frame.classList.add('active');

        // Enable buttons
        btnConfigure.disabled = false;
        btnApply.disabled = false;

        // Request saved settings for this screensaver
        sendToHost({ type: 'requestSettings', id: id });
    }

    // ── Configure button: open config page in system browser ──
    btnConfigure.addEventListener('click', function () {
        if (!pendingSelection) return;
        sendToHost({ type: 'navigateToConfig', id: pendingSelection });
    });

    // ── Apply button: save selection and close ──
    btnApply.addEventListener('click', function () {
        if (!pendingSelection) return;
        sendToHost({ type: 'selectScreensaver', id: pendingSelection });
        sendToHost({ type: 'closeConfig' });
    });
})();
