"use strict";

const canvas = document.getElementById('drawing-canvas');
const wrapper = document.getElementById('canvas-wrapper');
const viewport = document.getElementById('viewport');
const ctx = canvas.getContext('2d');
const colorPicker = document.getElementById('color-picker');
const brushSizeRange = document.getElementById('brush-size');
const sizeValueSpan = document.getElementById('size-value');
const clearBtn = document.getElementById('clear-btn');
const saveBtn = document.getElementById('save-btn');
const connectionStatus = document.getElementById('connection-status');
const connectionText = document.getElementById('connection-text');

// --- APP STATE ---
let drawing = false;
let lastX = 0;
let lastY = 0;
let isPanning = false;
let spacePressed = false;

// Viewport Transform
let transform = {
    x: 0,
    y: 0,
    scale: 0.8 // Start slightly zoomed out
};

// Multi-touch tracking
let initialPinchDistance = null;
let initialPinchScale = 1;
let lastMidpoint = { x: 0, y: 0 };
let lastTouchPos = { x: 0, y: 0 };
let currentStrokeId = null;
let strokeHistory = []; // Local history for redraws

// World Config
const WORLD_SIZE = 2500;
canvas.width = WORLD_SIZE;
canvas.height = WORLD_SIZE;

// --- INITIALIZE ---
function init() {
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    
    // Center initially
    const vw = viewport.clientWidth;
    const vh = viewport.clientHeight;
    transform.x = (vw - WORLD_SIZE * transform.scale) / 2;
    transform.y = (vh - WORLD_SIZE * transform.scale) / 2;
    applyTransform();
}

function applyTransform() {
    wrapper.style.transform = `translate(${transform.x}px, ${transform.y}px) scale(${transform.scale})`;
}

// --- SIGNALR SETUP ---
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/drawingHub")
    .withAutomaticReconnect()
    .build();

connection.on("ReceiveDraw", (data) => {
    strokeHistory.push(data);
    drawLine(data);
});
connection.on("CanvasCleared", () => {
    strokeHistory = [];
    clearLocal();
    bgLayer.src = "";
    bgLayer.style.display = 'none';
    bgOptions.style.display = 'none';
});
connection.on("ReceiveBackground", (base64) => {
    console.log("Received background update, length:", base64?.length || 0);
    updateBackgroundLocal(base64);
});
connection.on("LoadHistory", (history) => {
    strokeHistory = Array.isArray(history) ? history : [];
    redrawCanvas();
});
connection.on("StrokeUndone", (strokeId) => {
    console.log("Stroke undone:", strokeId);
    strokeHistory = strokeHistory.filter(s => {
        const sid = s.strokeId || s.StrokeId;
        return sid !== strokeId;
    });
    redrawCanvas();
});

connection.start().then(() => updateStatus('online', 'Connected')).catch(e => updateStatus('offline', 'Error'));

function updateStatus(status, text) {
    if (!connectionStatus) return;
    connectionStatus.className = `status-indicator ${status}`;
    connectionText.textContent = text;
}

// --- COORDINATE MAPPING ---
function getCoordinates(e) {
    const rect = canvas.getBoundingClientRect();
    let clientX, clientY;
    
    if (e.touches && e.touches.length > 0) {
        clientX = e.touches[0].clientX;
        clientY = e.touches[0].clientY;
    } else {
        clientX = e.clientX;
        clientY = e.clientY;
    }
    
    const x = (clientX - rect.left) * (canvas.width / rect.width);
    const y = (clientY - rect.top) * (canvas.height / rect.height);
    return [x, y];
}

// --- TOOL STATE ---
let currentTool = 'pencil'; // 'pencil', 'eraser', 'move'
let lastSelectedColor = '#3a86ff';

const pencilBtn = document.getElementById('pencil-tool');
const eraserBtn = document.getElementById('eraser-tool');
const moveBtn = document.getElementById('move-tool');

function setTool(tool) {
    currentTool = tool;
    pencilBtn.classList.toggle('active', tool === 'pencil');
    eraserBtn.classList.toggle('active', tool === 'eraser');
    moveBtn.classList.toggle('active', tool === 'move');
    
    // Set appropriate cursor
    if (tool === 'move') viewport.style.cursor = 'grab';
    else if (tool === 'eraser') viewport.style.cursor = 'cell';
    else viewport.style.cursor = 'crosshair';
}

pencilBtn.onclick = () => setTool('pencil');
eraserBtn.onclick = () => setTool('eraser');
moveBtn.onclick = () => setTool('move');

// --- INTERACTION HANDLERS ---
function startInteraction(e) {
    const isTouch = e.touches && e.touches.length > 0;
    const touchCount = isTouch ? e.touches.length : 1;

    // Reset multi-touch state
    if (touchCount === 2) {
        isPanning = true;
        drawing = false; // Never draw with 2 fingers
        const t1 = e.touches[0];
        const t2 = e.touches[1];
        initialPinchDistance = Math.hypot(t1.clientX - t2.clientX, t1.clientY - t2.clientY);
        initialPinchScale = transform.scale;
        lastMidpoint = { 
            x: (t1.clientX + t2.clientX) / 2, 
            y: (t1.clientY + t2.clientY) / 2 
        };
        return;
    }

    // 1-Finger/Mouse logic
    if (currentTool === 'move' || e.button === 1 || spacePressed) {
        isPanning = true;
        drawing = false;
        if (isTouch) {
            lastTouchPos = { x: e.touches[0].clientX, y: e.touches[0].clientY };
        }
    } else {
        drawing = true;
        isPanning = false;
        currentStrokeId = Date.now().toString() + Math.random().toString(36).substr(2, 9);
        const [x, y] = getCoordinates(e);
        lastX = x;
        lastY = y;
    }
}

function stopInteraction() {
    drawing = false;
    isPanning = false;
    initialPinchDistance = null;
    lastMidpoint = null;
    currentStrokeId = null;
}

function handleMove(e) {
    const isTouch = e.touches && e.touches.length > 0;
    const touchCount = isTouch ? e.touches.length : 1;

    // Handle 2-Finger Zoom ONLY
    if (isTouch && touchCount === 2) {
        handlePinchOnly(e);
        return;
    }

    // Handle Panning (1 finger or mouse)
    if (isPanning) {
        let dx, dy;
        if (isTouch) {
            dx = e.touches[0].clientX - lastTouchPos.x;
            dy = e.touches[0].clientY - lastTouchPos.y;
            lastTouchPos = { x: e.touches[0].clientX, y: e.touches[0].clientY };
        } else {
            dx = e.movementX;
            dy = e.movementY;
        }
        transform.x += dx;
        transform.y += dy;
        applyTransform();
        return;
    }

    // Handle Drawing
    if (drawing) {
        const [x, y] = getCoordinates(e);
        const isEraser = (currentTool === 'eraser');
        const drawData = {
            lastX, lastY, x, y,
            color: isEraser ? '#000000' : colorPicker.value, // Black doesn't matter for eraser
            size: parseInt(brushSizeRange.value),
            isEraser: isEraser,
            strokeId: currentStrokeId
        };

        strokeHistory.push(drawData);
        drawLine(drawData);
        if (connection.state === "Connected") {
            connection.invoke("DrawLine", drawData).catch(err => console.error(err));
        }
        [lastX, lastY] = [x, y];
    }
}

// Pinch & Zoom Only (Translation component removed for 2 fingers)
function handlePinchOnly(e) {
    const t1 = e.touches[0];
    const t2 = e.touches[1];
    const midX = (t1.clientX + t2.clientX) / 2;
    const midY = (t1.clientY + t2.clientY) / 2;
    const dist = Math.hypot(t1.clientX - t2.clientX, t1.clientY - t2.clientY);

    if (initialPinchDistance === null) {
        initialPinchDistance = dist;
        initialPinchScale = transform.scale;
    } else {
        const factor = dist / initialPinchDistance;
        const newScale = Math.min(Math.max(initialPinchScale * factor, 0.05), 10);
        
        // Zoom centered at midpoint, but NO additional translation (dx/dy)
        zoomAt(midX, midY, newScale);
        applyTransform();
    }
}

function zoomAt(clientX, clientY, newScale) {
    const oldScale = transform.scale;
    const mouseX = clientX - transform.x;
    const mouseY = clientY - transform.y;
    transform.x = clientX - (mouseX / oldScale) * newScale;
    transform.y = clientY - (mouseY / oldScale) * newScale;
    transform.scale = newScale;
}

// Event Listeners
viewport.addEventListener('wheel', (e) => {
    e.preventDefault();
    const delta = e.deltaY > 0 ? 0.95 : 1.05;
    zoomAt(e.clientX, e.clientY, transform.scale * delta);
    applyTransform();
}, { passive: false });

// Global Events
canvas.addEventListener('mousedown', startInteraction);
window.addEventListener('mousemove', handleMove);
window.addEventListener('mouseup', stopInteraction);

canvas.addEventListener('touchstart', (e) => {
    e.preventDefault();
    startInteraction(e);
}, { passive: false });

window.addEventListener('touchmove', (e) => {
    e.preventDefault();
    handleMove(e);
}, { passive: false });

window.addEventListener('touchend', stopInteraction);

// Utilities
function drawLine(data) {
    if (!data) return;
    
    // Normalize properties for both camelCase and PascalCase
    const lx = data.lastX !== undefined ? data.lastX : data.LastX;
    const ly = data.lastY !== undefined ? data.lastY : data.LastY;
    const x = data.x !== undefined ? data.x : data.X;
    const y = data.y !== undefined ? data.y : data.Y;
    const size = data.size !== undefined ? data.size : data.Size;
    const isEraser = data.isEraser !== undefined ? data.isEraser : data.IsEraser;
    const color = data.color !== undefined ? data.color : data.Color;

    ctx.beginPath();
    ctx.moveTo(lx, ly);
    ctx.lineTo(x, y);
    
    if (isEraser) {
        ctx.globalCompositeOperation = 'destination-out';
    } else {
        ctx.globalCompositeOperation = 'source-over';
    }
    
    ctx.strokeStyle = color;
    ctx.lineWidth = size;
    ctx.stroke();
    ctx.closePath();
    
    // Reset composite operation
    ctx.globalCompositeOperation = 'source-over';
}

function redrawCanvas() {
    clearLocal();
    strokeHistory.forEach(s => drawLine(s));
}

function clearLocal() {
    ctx.clearRect(0, 0, canvas.width, canvas.height);
}

const palette = document.getElementById('palette');
const paletteEditor = document.getElementById('palette-editor');
let longPressTimer;
let currentEditingSwatch = null;

palette.onclick = (e) => {
    if (e.target.classList.contains('swatch')) {
        const color = e.target.dataset.color;
        colorPicker.value = color;
        setTool('pencil');
    }
};

// --- PALETTE EDITING (Right Click & Long Press) ---
palette.oncontextmenu = (e) => {
    if (e.target.classList.contains('swatch')) {
        e.preventDefault();
        openSwatchEditor(e.target);
    }
};

// Long Press for Android/iOS
palette.addEventListener('touchstart', (e) => {
    if (e.target.classList.contains('swatch')) {
        longPressTimer = setTimeout(() => {
            openSwatchEditor(e.target);
            longPressTimer = null;
        }, 600); // 600ms for long press
    }
}, { passive: true });

palette.addEventListener('touchend', () => {
    if (longPressTimer) {
        clearTimeout(longPressTimer);
        longPressTimer = null;
    }
});

palette.addEventListener('touchmove', () => {
    if (longPressTimer) {
        clearTimeout(longPressTimer);
        longPressTimer = null;
    }
});

function openSwatchEditor(swatch) {
    currentEditingSwatch = swatch;
    paletteEditor.value = swatch.dataset.color;
    paletteEditor.click();
}

paletteEditor.oninput = () => {
    if (currentEditingSwatch) {
        const newColor = paletteEditor.value;
        currentEditingSwatch.style.background = newColor;
        currentEditingSwatch.dataset.color = newColor;
        // Optionally select it immediately
        colorPicker.value = newColor;
        setTool('pencil');
    }
};

// Tooling & Actions
colorPicker.oninput = () => { if (currentTool === 'eraser') setTool('pencil'); };
brushSizeRange.oninput = () => { sizeValueSpan.textContent = brushSizeRange.value; };

const undoBtn = document.getElementById('undo-btn');

function undo() {
    console.log("Undo requested");
    if (connection.state === "Connected") {
        connection.invoke("UndoStroke").catch(err => console.error("Undo error:", err));
    } else {
        console.warn("Cannot undo: Connection state is", connection.state);
    }
}

undoBtn.onclick = undo;

clearBtn.onclick = () => {
    if (confirm('Clear canvas for everyone?')) connection.invoke("ClearCanvas");
};

saveBtn.onclick = () => {
    // Create temp canvas for merging
    const tempCanvas = document.createElement('canvas');
    tempCanvas.width = canvas.width;
    tempCanvas.height = canvas.height;
    const tCtx = tempCanvas.getContext('2d');

    // 1. Draw Background Image if active
    if (bgLayer.src && bgLayer.style.display !== 'none') {
        tCtx.globalAlpha = parseFloat(bgLayer.style.opacity) || 0.5;
        
        // Calculate "object-fit: contain" for the canvas export
        const canvasW = tempCanvas.width;
        const canvasH = tempCanvas.height;
        const imgW = bgLayer.naturalWidth;
        const imgH = bgLayer.naturalHeight;
        
        const ratio = Math.min(canvasW / imgW, canvasH / imgH);
        const drawW = imgW * ratio;
        const drawH = imgH * ratio;
        const drawX = (canvasW - drawW) / 2;
        const drawY = (canvasH - drawH) / 2;

        tCtx.drawImage(bgLayer, drawX, drawY, drawW, drawH);
        tCtx.globalAlpha = 1.0;
    }

    // 2. Draw Drawing Layer
    tCtx.drawImage(canvas, 0, 0);

    const link = document.createElement('a');
    link.download = `scribble-${Date.now()}.png`;
    link.href = tempCanvas.toDataURL();
    link.click();
};

const toggleUiBtn = document.getElementById('toggle-ui');
let uiHidden = false;

function toggleUI() {
    uiHidden = !uiHidden;
    document.body.classList.toggle('tools-hidden', uiHidden);
    toggleUiBtn.querySelector('i').className = uiHidden ? 'fa-solid fa-eye-slash' : 'fa-solid fa-eye';
    toggleUiBtn.classList.toggle('active', uiHidden);
}

toggleUiBtn.onclick = toggleUI;

window.onload = init;
window.onresize = applyTransform;

// --- BACKGROUND LAYER MANAGEMENT ---
const addBgBtn = document.getElementById('add-bg-btn');
const bgUpload = document.getElementById('bg-upload');
const bgLayer = document.getElementById('bg-layer');
const bgOptions = document.getElementById('bg-options');
const bgOpacityRange = document.getElementById('bg-opacity');

if (addBgBtn) {
    addBgBtn.onclick = () => {
        const hasBg = bgLayer.src && bgLayer.style.display !== 'none';
        if (hasBg) {
            // Remove Background
            if (confirm("Remove background for everyone?")) {
                bgLayer.src = "";
                bgLayer.style.display = 'none';
                bgOptions.style.display = 'none';
                addBgBtn.querySelector('i').className = 'fa-solid fa-layer-group';
                if (connection.state === "Connected") {
                    connection.invoke("UpdateBackground", "");
                }
            }
        } else {
            bgUpload.click();
        }
    };
}

if (bgUpload) {
    bgUpload.addEventListener('change', (e) => {
        const file = e.target.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = (event) => {
                const base64 = event.target.result;
                updateBackgroundLocal(base64);
                if (connection.state === "Connected") {
                    connection.invoke("UpdateBackground", base64);
                }
            };
            reader.readAsDataURL(file);
        }
    });
}

function updateBackgroundLocal(base64) {
    if (!base64) {
        bgLayer.src = "";
        bgLayer.style.display = 'none';
        bgOptions.style.display = 'none';
        addBgBtn.querySelector('i').className = 'fa-solid fa-layer-group';
    } else {
        bgLayer.src = base64;
        bgLayer.style.display = 'block';
        bgOptions.style.display = 'flex';
        addBgBtn.querySelector('i').className = 'fa-solid fa-trash-can'; // Icon change to trash when active
    }
}

if (bgOpacityRange) {
    bgOpacityRange.oninput = () => {
        if (bgLayer) bgLayer.style.opacity = bgOpacityRange.value;
    };
}

// Update shortcuts
window.onkeydown = (e) => {
    if (e.code === 'Space') { spacePressed = true; viewport.style.cursor = 'grab'; }
    if (e.code === 'Tab') { e.preventDefault(); toggleUI(); }
    if (e.key.toLowerCase() === 'z' && (e.ctrlKey || e.metaKey)) { e.preventDefault(); undo(); }
    if (e.key.toLowerCase() === 'b') setTool('pencil');
    if (e.key.toLowerCase() === 'e') setTool('eraser');
    if (e.key.toLowerCase() === 'h') setTool('move');
    if (e.key.toLowerCase() === 'l') bgUpload?.click();
};

window.onkeyup = (e) => {
    if (e.code === 'Space') { spacePressed = false; setTool(currentTool); }
};
