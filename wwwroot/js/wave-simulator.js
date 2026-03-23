let chartInstance = null;
const functionsContainer = document.getElementById('functionsContainer');
const addFuncBtn = document.getElementById('addFuncBtn');

const DEFAULT_COLORS = ['#22d3ee', '#a78bfa', '#fb7185', '#34d399', '#fbbf24', '#38bdf8', '#ff00ff', '#ffffff'];

function createFunctionRow(value = '', color = null) {
    if (!color) {
        const count = document.querySelectorAll('.func-row').length;
        color = DEFAULT_COLORS[count % DEFAULT_COLORS.length];
    }
    
    const div = document.createElement('div');
    div.className = 'func-row flex items-center gap-3 group';
    div.innerHTML = `
        <div class="flex-grow relative">
            <div class="absolute inset-y-0 left-0 pl-3 flex items-center">
                <input type="color" class="colorPicker w-6 h-6 bg-transparent border-none cursor-pointer p-0 rounded-full overflow-hidden" 
                       value="${color}" title="Custom Color">
            </div>
            <input type="text" class="funcInput w-full bg-slate-800/50 border border-slate-700 rounded-xl pl-11 pr-4 py-3 text-white focus:outline-none focus:border-cyan-500/50 focus:ring-1 focus:ring-cyan-500/20 font-mono text-base transition-all" 
                   value="${value}" placeholder="e.g., sin(t)">
        </div>
        <button class="removeFuncBtn opacity-0 group-hover:opacity-100 p-2 text-slate-500 hover:text-red-400 transition-all transform hover:scale-110" title="Remove">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
        </button>
    `;
    
    const input = div.querySelector('.funcInput');
    const colorPicker = div.querySelector('.colorPicker');
    const removeBtn = div.querySelector('.removeFuncBtn');

    input.addEventListener('keypress', (e) => { if (e.key === 'Enter') simulate(); });
    colorPicker.addEventListener('input', simulate);
    
    removeBtn.addEventListener('click', () => {
        if (document.querySelectorAll('.func-row').length > 1) {
            div.style.opacity = '0';
            div.style.transform = 'translateX(20px)';
            setTimeout(() => {
                div.remove();
                simulate();
            }, 300);
        } else {
            input.value = '';
            simulate();
        }
    });
    
    return div;
}

addFuncBtn.addEventListener('click', () => {
    const nextColor = DEFAULT_COLORS[document.querySelectorAll('.func-row').length % DEFAULT_COLORS.length];
    const newRow = createFunctionRow('', nextColor);
    functionsContainer.appendChild(newRow);
    newRow.querySelector('input').focus();
});

// Initialize with default function
if (functionsContainer) {
    functionsContainer.appendChild(createFunctionRow('sin(2 * pi * t)'));
}

// Auto update labels and default ranges depending on transform type
const analysisTypeSelect = document.getElementById('analysisType');
if (analysisTypeSelect) {
    analysisTypeSelect.addEventListener('change', function(e) {
        const type = e.target.value;
        const lblMin = document.getElementById('lblMin');
        const lblMax = document.getElementById('lblMax');
        const xMin = document.getElementById('xMin');
        const xMax = document.getElementById('xMax');
        const step = document.getElementById('step');

        if (type === 'time') {
            lblMin.innerText = 'Min (t)'; lblMax.innerText = 'Max (t)';
            xMin.value = '0'; xMax.value = '10'; step.value = '0.05';
        } else if (type === 'fourier') {
            lblMin.innerText = 'Min (ω)'; lblMax.innerText = 'Max (ω)';
            xMin.value = '-10'; xMax.value = '10'; step.value = '0.1';
        } else if (type === 'freq_mag' || type === 'freq_phase') {
            lblMin.innerText = 'Min (ω)'; lblMax.innerText = 'Max (ω)';
            xMin.value = '0.1'; xMax.value = '20'; step.value = '0.1';
        } else if (type === 'laplace') {
            lblMin.innerText = 'Min (s)'; lblMax.innerText = 'Max (s)';
            xMin.value = '0.1'; xMax.value = '5'; step.value = '0.1';
        }
        simulate();
    });
}

function hexToRgba(hex, alpha) {
    let r = parseInt(hex.slice(1, 3), 16);
    let g = parseInt(hex.slice(3, 5), 16);
    let b = parseInt(hex.slice(5, 7), 16);
    return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

function simulate() {
    const rows = document.querySelectorAll('.func-row');
    const xMinEl = document.getElementById('xMin');
    const xMaxEl = document.getElementById('xMax');
    const stepEl = document.getElementById('step');
    const analysisTypeEl = document.getElementById('analysisType');
    
    if (!xMinEl || !xMaxEl || !stepEl || !analysisTypeEl) return;

    const xMin = parseFloat(xMinEl.value);
    const xMax = parseFloat(xMaxEl.value);
    const step = parseFloat(stepEl.value);
    const analysisType = analysisTypeEl.value;

    // Y Axis Settings
    const autoY = document.getElementById('autoY').checked;
    const yMin = parseFloat(document.getElementById('yMin').value);
    const yMax = parseFloat(document.getElementById('yMax').value);

    if (xMin >= xMax || step <= 0) return;

    const allDatasets = [];
    let globalXValues = null;

    rows.forEach((row) => {
        const input = row.querySelector('.funcInput');
        const colorInput = row.querySelector('.colorPicker');
        const funcStr = input.value.trim();
        if (!funcStr) return;

        const xValues = [];
        const yValues = [];
        const colorHex = colorInput.value;

        try {
            const node = math.parse(funcStr);
            const code = node.compile();
            const evalFunc = (t_val) => {
                try {
                    let res = code.evaluate({ t: t_val, x: t_val });
                    return isFinite(res) ? res : 0;
                } catch { return 0; }
            };

            if (analysisType === 'time') {
                for (let t = xMin; t <= xMax; t += step) {
                    const val = Math.round(t * 1000) / 1000;
                    xValues.push(val);
                    yValues.push(evalFunc(val));
                }
            } 
            else if (analysisType === 'fourier' || analysisType === 'freq_mag' || analysisType === 'freq_phase') {
                const tMin = -50, tMax = 50, dt = 0.1;
                for (let w = xMin; w <= xMax; w += step) {
                    let re = 0, im = 0;
                    for (let t = tMin; t <= tMax; t += dt) {
                        let ft = evalFunc(t);
                        re += ft * Math.cos(w * t) * dt;
                        im -= ft * Math.sin(w * t) * dt;
                    }
                    const magnitude = Math.sqrt(re*re + im*im);
                    xValues.push(Math.round(w * 1000) / 1000);
                    
                    if (analysisType === 'fourier') yValues.push(magnitude);
                    else if (analysisType === 'freq_mag') yValues.push(magnitude > 1e-10 ? 20 * Math.log10(magnitude) : -100);
                    else if (analysisType === 'freq_phase') yValues.push(Math.atan2(im, re));
                }
            }
            else if (analysisType === 'laplace') {
                const tMaxL = 50, dt = 0.1;
                for (let s = xMin; s <= xMax; s += step) {
                    let integral = 0;
                    for (let t = 0; t <= tMaxL; t += dt) {
                        integral += evalFunc(t) * Math.exp(-s * t) * dt;
                    }
                    xValues.push(Math.round(s * 1000) / 1000);
                    yValues.push(integral);
                }
            }

            if (!globalXValues) globalXValues = xValues;

            allDatasets.push({
                label: funcStr,
                data: yValues,
                borderColor: colorHex,
                backgroundColor: hexToRgba(colorHex, 0.1),
                borderWidth: 3,
                pointRadius: 0,
                pointHoverRadius: 5,
                fill: analysisType !== 'freq_mag',
                tension: 0.4,
                borderCapStyle: 'round',
            });

        } catch (err) {
            console.error("Math error:", err);
        }
    });

    if (allDatasets.length > 0) {
        drawChart(globalXValues, allDatasets, analysisType, autoY, yMin, yMax);
    }
}

function drawChart(xValues, datasets, type, autoY, yMin, yMax) {
    const canvas = document.getElementById('waveChart');
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    
    if (chartInstance) chartInstance.destroy();

    const xLabel = type === 'time' ? 'Time (t)' : 'Domain Variable';
    const yLabel = type === 'time' ? 'Amplitude f(t)' : 'Magnitude / Value';

    const yAxisConfig = {
        title: { display: true, text: yLabel, color: '#64748b' },
        grid: { color: 'rgba(255, 255, 255, 0.05)' },
        ticks: { color: '#64748b' }
    };

    if (!autoY) {
        yAxisConfig.min = yMin;
        yAxisConfig.max = yMax;
    }

    chartInstance = new Chart(ctx, {
        type: 'line',
        data: { labels: xValues, datasets: datasets },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: { intersect: false, mode: 'index' },
            scales: {
                x: {
                    title: { display: true, text: xLabel, color: '#64748b' },
                    grid: { color: 'rgba(255, 255, 255, 0.03)' },
                    ticks: { color: '#64748b', maxTicksLimit: 10 }
                },
                y: yAxisConfig
            },
            plugins: {
                legend: { position: 'top', labels: { color: '#f8fafc', boxWidth: 12, usePointStyle: true, font: { size: 12 } } },
                tooltip: { backgroundColor: 'rgba(15, 23, 42, 0.9)', padding: 12, cornerRadius: 10 }
            }
        }
    });
}

// Toggle Y Controls
const autoYCheckbox = document.getElementById('autoY');
const yControlRow = document.getElementById('yControlRow');
if (autoYCheckbox && yControlRow) {
    autoYCheckbox.addEventListener('change', () => {
        if (autoYCheckbox.checked) {
            yControlRow.classList.add('opacity-40', 'pointer-events-none');
        } else {
            yControlRow.classList.remove('opacity-40', 'pointer-events-none');
        }
        simulate();
    });
}

const yMinInput = document.getElementById('yMin');
if (yMinInput) yMinInput.addEventListener('input', simulate);

const yMaxInput = document.getElementById('yMax');
if (yMaxInput) yMaxInput.addEventListener('input', simulate);

const simulateBtn = document.getElementById('simulateBtn');
if (simulateBtn) simulateBtn.addEventListener('click', simulate);

// Initial simulation
setTimeout(simulate, 100);
