import * as pdfjsLib from 'https://cdn.jsdelivr.net/npm/pdfjs-dist@4.10.38/build/pdf.min.mjs';

pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdn.jsdelivr.net/npm/pdfjs-dist@4.10.38/build/pdf.worker.min.mjs';

const shell = document.getElementById('pdfPreviewShell');

if (shell) {
    const canvas = document.getElementById('pdfPreviewCanvas');
    const overlay = document.getElementById('pdfPreviewOverlay');
    const status = document.getElementById('pdfPreviewStatus');
    const pageInput = document.getElementById('AddField_PageNumber');
    const xInput = document.getElementById('AddField_X');
    const yInput = document.getElementById('AddField_Y');
    const pdfUrl = shell.dataset.pdfUrl;
    const currentPageFromMarkup = Number(shell.dataset.currentPage || '1');
    const fields = JSON.parse(shell.dataset.fields || '[]');

    if (!(canvas instanceof HTMLCanvasElement) || !(overlay instanceof HTMLDivElement) || !pdfUrl) {
        if (status) {
            status.textContent = 'Preview unavailable.';
        }
    } else {
        const context = canvas.getContext('2d');
        let pdfDocument = null;
        let currentPageNumber = Number.isFinite(currentPageFromMarkup) && currentPageFromMarkup > 0 ? currentPageFromMarkup : 1;
        let resizeTimer = 0;

        const getFieldInputs = () => ({
            page: pageInput,
            x: xInput,
            y: yInput,
        });

        const setStatus = (message) => {
            if (status) {
                status.textContent = message;
            }
        };

        const renderMarkers = () => {
            overlay.replaceChildren();

            const rect = canvas.getBoundingClientRect();
            if (!rect.width || !rect.height || !canvas.width || !canvas.height) {
                return;
            }

            const xScale = rect.width / canvas.width;
            const yScale = rect.height / canvas.height;

            fields
                .filter((field) => field.pageNumber === currentPageNumber)
                .forEach((field) => {
                    const marker = document.createElement('div');
                    marker.className = `pdf-field-marker${field.isRequired ? ' required' : ''}`;
                    marker.style.left = `${field.x * xScale}px`;
                    marker.style.top = `${(canvas.height - field.y - field.height) * yScale}px`;
                    marker.style.width = `${Math.max(field.width * xScale, 24)}px`;
                    marker.style.height = `${Math.max(field.height * yScale, 18)}px`;
                    marker.title = `${field.label} · ${field.isRequired ? 'Required' : 'Optional'} · Page ${field.pageNumber}`;
                    marker.textContent = field.label;
                    overlay.appendChild(marker);
                });
        };

        const renderPage = async (pageNumber) => {
            if (!pdfDocument) {
                pdfDocument = await pdfjsLib.getDocument(pdfUrl).promise;
            }

            const safePageNumber = Math.min(Math.max(1, Number(pageNumber) || 1), pdfDocument.numPages);
            currentPageNumber = safePageNumber;

            const page = await pdfDocument.getPage(safePageNumber);
            const viewport = page.getViewport({ scale: 1 });

            canvas.width = Math.floor(viewport.width);
            canvas.height = Math.floor(viewport.height);

            if (!context) {
                throw new Error('Canvas rendering context is unavailable.');
            }

            setStatus(`Rendering page ${safePageNumber} of ${pdfDocument.numPages}…`);

            await page.render({ canvasContext: context, viewport }).promise;
            renderMarkers();
            setStatus(`Showing page ${safePageNumber} of ${pdfDocument.numPages}. Click the page to set X/Y.`);
        };

        const updatePageNumber = () => {
            if (!pageInput) {
                return;
            }

            const parsed = Number(pageInput.value);
            if (!Number.isFinite(parsed) || parsed < 1) {
                pageInput.value = String(currentPageNumber);
                return;
            }

            renderPage(parsed).catch((error) => {
                console.error(error);
                setStatus('Unable to render that page.');
            });
        };

        shell.addEventListener('click', (event) => {
            if (!context) {
                return;
            }

            const rect = canvas.getBoundingClientRect();
            if (!rect.width || !rect.height) {
                return;
            }

            const clickX = Math.max(0, Math.min(rect.width, event.clientX - rect.left));
            const clickY = Math.max(0, Math.min(rect.height, event.clientY - rect.top));

            const pdfX = (clickX / rect.width) * canvas.width;
            const pdfY = canvas.height - ((clickY / rect.height) * canvas.height);

            const inputs = getFieldInputs();
            if (inputs.x) {
                inputs.x.value = pdfX.toFixed(2);
            }

            if (inputs.y) {
                inputs.y.value = pdfY.toFixed(2);
            }

            setStatus(`Placed the next field start at X ${pdfX.toFixed(2)}, Y ${pdfY.toFixed(2)}.`);
        });

        if (pageInput) {
            pageInput.addEventListener('change', updatePageNumber);
        }

        if (typeof ResizeObserver !== 'undefined') {
            const observer = new ResizeObserver(() => {
                window.clearTimeout(resizeTimer);
                resizeTimer = window.setTimeout(() => {
                    renderMarkers();
                }, 50);
            });

            observer.observe(shell);
        } else {
            window.addEventListener('resize', () => renderMarkers());
        }

        renderPage(currentPageNumber).catch((error) => {
            console.error(error);
            setStatus('Unable to load the preview.');
        });
    }
}
