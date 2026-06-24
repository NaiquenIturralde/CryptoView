// Interactividad para ventana modal de notas (drag, minimizar, maximizar)
window.cryptoNotesModal = (function () {
    let dragData = null;
    let lastPosition = { x: 0, y: 0 };
    let lastSize = { width: 0, height: 0 };
    let isMaximized = false;
    let isMinimized = false;
    let modalEl = null;
    let headerEl = null;
    let contentEl = null;
    let minimizedBar = null;

    function clamp(val, min, max) {
        return Math.max(min, Math.min(max, val));
    }

    function onMouseDown(e) {
        if (isMaximized || isMinimized) return;
        dragData = {
            startX: e.clientX,
            startY: e.clientY,
            origX: modalEl.offsetLeft,
            origY: modalEl.offsetTop
        };
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
        modalEl.classList.add('dragging');
    }

    function onMouseMove(e) {
        if (!dragData) return;
        let dx = e.clientX - dragData.startX;
        let dy = e.clientY - dragData.startY;
        let newX = clamp(dragData.origX + dx, 0, window.innerWidth - modalEl.offsetWidth);
        let newY = clamp(dragData.origY + dy, 0, window.innerHeight - 60);
        modalEl.style.left = newX + 'px';
        modalEl.style.top = newY + 'px';
        modalEl.style.right = 'auto';
        modalEl.style.bottom = 'auto';
    }

    function onMouseUp() {
        if (!dragData) return;
        lastPosition.x = modalEl.offsetLeft;
        lastPosition.y = modalEl.offsetTop;
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);
        modalEl.classList.remove('dragging');
        dragData = null;
    }

    function minimize() {
        if (isMinimized) return;
        isMinimized = true;
        contentEl.style.display = 'none';
        minimizedBar.style.display = 'flex';
        modalEl.classList.add('minimized');
    }

    function restoreFromMinimize() {
        if (!isMinimized) return;
        isMinimized = false;
        contentEl.style.display = '';
        minimizedBar.style.display = 'none';
        modalEl.classList.remove('minimized');
    }

    function maximize() {
        if (isMaximized) return;
        lastPosition.x = modalEl.offsetLeft;
        lastPosition.y = modalEl.offsetTop;
        lastSize.width = modalEl.offsetWidth;
        lastSize.height = modalEl.offsetHeight;
        modalEl.style.left = '2vw';
        modalEl.style.top = '2vh';
        modalEl.style.width = '96vw';
        modalEl.style.height = '96vh';
        modalEl.style.right = 'auto';
        modalEl.style.bottom = 'auto';
        isMaximized = true;
        modalEl.classList.add('maximized');
    }

    function restoreFromMaximize() {
        if (!isMaximized) return;
        modalEl.style.left = lastPosition.x + 'px';
        modalEl.style.top = lastPosition.y + 'px';
        modalEl.style.width = lastSize.width + 'px';
        modalEl.style.height = lastSize.height + 'px';
        isMaximized = false;
        modalEl.classList.remove('maximized');
    }

    function close() {
        modalEl.style.display = 'none';
    }

    function setup(modalId) {
        modalEl = document.getElementById(modalId);
        if (!modalEl) return;
        headerEl = modalEl.querySelector('.crypto-notes-modal-header');
        contentEl = modalEl.querySelector('.crypto-notes-modal-content');
        minimizedBar = modalEl.querySelector('.crypto-notes-modal-minimized');
        if (headerEl) headerEl.addEventListener('mousedown', onMouseDown);
        if (minimizedBar) minimizedBar.addEventListener('click', restoreFromMinimize);
    }

    return {
        setup,
        minimize,
        maximize,
        restoreFromMaximize,
        restoreFromMinimize,
        close
    };
})();
