import * as pdfjsLib from 'https://cdn.jsdelivr.net/npm/pdfjs-dist@4.10.38/build/pdf.min.mjs';

pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdn.jsdelivr.net/npm/pdfjs-dist@4.10.38/build/pdf.worker.min.mjs';

const shell = document.getElementById('pdfPreviewShell');

if (shell) {
    const canvas = document.getElementById('pdfPreviewCanvas');
    const overlay = document.getElementById('pdfPreviewOverlay');
    const draftOverlay = document.getElementById('pdfPreviewDraft');
    const status = document.getElementById('pdfPreviewStatus');
    const addFieldForm = document.getElementById('addFieldForm');
    const pageInput = document.getElementById('AddField_PageNumber');
    const xInput = document.getElementById('AddField_X');
    const yInput = document.getElementById('AddField_Y');
    const widthInput = document.getElementById('AddField_Width');
    const heightInput = document.getElementById('AddField_Height');
    const emptyState = document.getElementById('signatureFieldEmptyState');
    const rows = document.getElementById('signatureFieldRows');
    const readyContainer = document.getElementById('markReadyContainer');
    const pdfUrl = shell.dataset.pdfUrl;
    const currentPageFromMarkup = Number(shell.dataset.currentPage || '1');
    const documentStatus = shell.dataset.documentStatus || '';
    const documentId = shell.dataset.documentId || '';
    const fields = JSON.parse(shell.dataset.fields || '[]');

    if (!(canvas instanceof HTMLCanvasElement) || !(overlay instanceof HTMLDivElement) || !(draftOverlay instanceof HTMLDivElement) || !pdfUrl) {
        if (status) {
            status.textContent = 'Preview unavailable.';
        }
    } else {
        const context = canvas.getContext('2d');
        const state = {
            pdfDocument: null,
            currentPageNumber: Number.isFinite(currentPageFromMarkup) && currentPageFromMarkup > 0 ? currentPageFromMarkup : 1,
            draft: null,
            drawing: false,
            pointerId: null,
            startPoint: null,
            isSubmitting: false,
        };

        const clamp = (value, min, max) => Math.min(Math.max(value, min), max);

        const setStatus = (message) => {
            if (status) {
                status.textContent = message;
            }
        };

        const getInputs = () => ({
            page: pageInput,
            x: xInput,
            y: yInput,
            width: widthInput,
            height: heightInput,
        });

        const getCanvasMetrics = () => {
            const rect = canvas.getBoundingClientRect();
            if (!rect.width || !rect.height || !canvas.width || !canvas.height) {
                return null;
            }

            return {
                rect,
                scaleX: rect.width / canvas.width,
                scaleY: rect.height / canvas.height,
            };
        };

        const toCanvasPoint = (clientX, clientY) => {
            const metrics = getCanvasMetrics();
            if (!metrics) {
                return null;
            }

            return {
                metrics,
                x: clamp(clientX - metrics.rect.left, 0, metrics.rect.width),
                y: clamp(clientY - metrics.rect.top, 0, metrics.rect.height),
            };
        };

        const toPdfRectFromCanvasPoints = (startPoint, endPoint) => {
            const left = Math.min(startPoint.x, endPoint.x);
            const right = Math.max(startPoint.x, endPoint.x);
            const top = Math.min(startPoint.y, endPoint.y);
            const bottom = Math.max(startPoint.y, endPoint.y);

            const width = (right - left) / startPoint.metrics.rect.width * canvas.width;
            const height = (bottom - top) / startPoint.metrics.rect.height * canvas.height;
            const x = left / startPoint.metrics.rect.width * canvas.width;
            const y = canvas.height - (bottom / startPoint.metrics.rect.height * canvas.height);

            return {
                x,
                y,
                width,
                height,
            };
        };

        const toCanvasRectFromPdfRect = (pdfRect) => {
            const metrics = getCanvasMetrics();
            if (!metrics) {
                return null;
            }

            const x = pdfRect.x * metrics.scaleX;
            const y = (canvas.height - pdfRect.y - pdfRect.height) * metrics.scaleY;
            const width = Math.max(pdfRect.width * metrics.scaleX, 1);
            const height = Math.max(pdfRect.height * metrics.scaleY, 1);

            return { x, y, width, height, metrics };
        };

        const createBoxElement = (className, text, pdfRect, title) => {
            const rect = toCanvasRectFromPdfRect(pdfRect);
            if (!rect) {
                return null;
            }

            const element = document.createElement('div');
            element.className = className;
            element.style.left = `${rect.x}px`;
            element.style.top = `${rect.y}px`;
            element.style.width = `${Math.max(rect.width, 24)}px`;
            element.style.height = `${Math.max(rect.height, 18)}px`;
            element.title = title;
            element.textContent = text;
            return element;
        };

        const updateEmptyState = () => {
            if (!emptyState) {
                return;
            }

            emptyState.classList.toggle('d-none', fields.length > 0);
        };

        const updateReadyButton = () => {
            if (!readyContainer) {
                return;
            }

            const shouldShow = documentStatus === 'Draft' && fields.length > 0;
            readyContainer.classList.toggle('d-none', !shouldShow);
        };

        const updateInputsFromDraft = (draft) => {
            const inputs = getInputs();

            if (inputs.page) {
                inputs.page.value = String(state.currentPageNumber);
            }

            if (inputs.x) {
                inputs.x.value = draft.x.toFixed(2);
            }

            if (inputs.y) {
                inputs.y.value = draft.y.toFixed(2);
            }

            if (inputs.width) {
                inputs.width.value = Math.max(draft.width, 0.01).toFixed(2);
            }

            if (inputs.height) {
                inputs.height.value = Math.max(draft.height, 0.01).toFixed(2);
            }
        };

        const normalizeDraft = (draft) => ({
            x: Math.max(0, draft.x),
            y: Math.max(0, draft.y),
            width: Math.max(0.01, draft.width),
            height: Math.max(0.01, draft.height),
        });

        const renderBoxes = () => {
            overlay.replaceChildren();
            draftOverlay.replaceChildren();

            const metrics = getCanvasMetrics();
            if (!metrics) {
                return;
            }

            fields
                .filter((field) => field.pageNumber === state.currentPageNumber)
                .forEach((field) => {
                    const pdfRect = {
                        x: Number(field.x),
                        y: Number(field.y),
                        width: Number(field.width),
                        height: Number(field.height),
                    };

                    const marker = createBoxElement(
                        `pdf-field-marker${field.isRequired ? ' required' : ''}`,
                        field.label,
                        pdfRect,
                        `${field.label} · ${field.isRequired ? 'Required' : 'Optional'} · Page ${field.pageNumber}`,
                    );

                    if (marker) {
                        overlay.appendChild(marker);
                    }
                });

            if (state.draft) {
                const draftRect = normalizeDraft(state.draft);
                const draft = createBoxElement(
                    'pdf-field-draft',
                    'Drawing field',
                    draftRect,
                    `Draft field · ${draftRect.width.toFixed(1)} × ${draftRect.height.toFixed(1)}`,
                );

                if (draft) {
                    draft.dataset.size = `${draftRect.width.toFixed(1)} × ${draftRect.height.toFixed(1)}`;
                    draftOverlay.appendChild(draft);
                }
            }
        };

        const setDraft = (draft) => {
            state.draft = draft ? normalizeDraft(draft) : null;

            if (state.draft) {
                updateInputsFromDraft(state.draft);
                setStatus(`Drawing box on page ${state.currentPageNumber}…`);
            }

            renderBoxes();
        };

        const renderPage = async (pageNumber) => {
            if (!state.pdfDocument) {
                state.pdfDocument = await pdfjsLib.getDocument(pdfUrl).promise;
            }

            const safePageNumber = Math.min(Math.max(1, Number(pageNumber) || 1), state.pdfDocument.numPages);
            state.currentPageNumber = safePageNumber;

            const page = await state.pdfDocument.getPage(safePageNumber);
            const viewport = page.getViewport({ scale: 1 });

            canvas.width = Math.floor(viewport.width);
            canvas.height = Math.floor(viewport.height);

            if (!context) {
                throw new Error('Canvas rendering context is unavailable.');
            }

            setStatus(`Rendering page ${safePageNumber} of ${state.pdfDocument.numPages}…`);

            await page.render({ canvasContext: context, viewport }).promise;
            renderBoxes();
            setStatus(`Showing page ${safePageNumber} of ${state.pdfDocument.numPages}. Drag to draw a field.`);
        };

        const submitFieldForm = async () => {
            if (!addFieldForm || state.isSubmitting) {
                return;
            }

            state.isSubmitting = true;
            setStatus('Saving field…');

            try {
                const formData = new FormData(addFieldForm);
                const response = await fetch(addFieldForm.action || window.location.href, {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest',
                        Accept: 'application/json',
                    },
                    credentials: 'same-origin',
                });

                const contentType = response.headers.get('content-type') || '';
                const payload = contentType.includes('application/json')
                    ? await response.json()
                    : { message: await response.text() };

                if (!response.ok) {
                    throw new Error(payload?.message || 'Unable to save the field.');
                }

                const savedField = payload?.field;
                if (!savedField) {
                    throw new Error('The server did not return the saved field.');
                }

                fields.push(savedField);
                state.draft = null;
                renderBoxes();
                updateEmptyState();
                updateReadyButton();
                appendFieldRow(savedField);
                setStatus(payload?.message || `Added ${savedField.label}.`);
            } catch (error) {
                console.error(error);
                setStatus(error instanceof Error ? error.message : 'Unable to save the field.');
            } finally {
                state.isSubmitting = false;
            }
        };

        const removeFieldById = (fieldId) => {
            const normalizedFieldId = String(fieldId);
            const index = fields.findIndex((field) => String(field.id) === normalizedFieldId);

            if (index >= 0) {
                fields.splice(index, 1);
            }

            const entry = rows?.querySelector(`[data-field-id="${CSS.escape(normalizedFieldId)}"]`);
            if (entry) {
                entry.remove();
            }

            state.draft = null;
            renderBoxes();
            updateEmptyState();
            updateReadyButton();
        };

        const appendFieldRow = (field) => {
            if (!rows) {
                return;
            }

            const entry = document.createElement('div');
            entry.className = 'field-entry rounded-4 p-3';
            entry.dataset.fieldId = field.id;

            const header = document.createElement('div');
            header.className = 'd-flex align-items-start justify-content-between gap-3';

            const heading = document.createElement('div');
            const label = document.createElement('div');
            label.className = 'fw-semibold';
            label.textContent = field.label;
            heading.appendChild(label);

            const required = document.createElement('div');
            required.className = 'small text-white-50';
            required.textContent = field.isRequired ? 'Required' : 'Optional';
            heading.appendChild(required);

            const actionCell = document.createElement('form');
            actionCell.method = 'post';
            actionCell.dataset.deleteFieldForm = 'true';
            actionCell.className = 'ms-auto';
            const actionUrl = new URL(window.location.href);
            actionUrl.searchParams.set('handler', 'DeleteField');
            actionUrl.searchParams.set('signatureFieldId', field.id);
            actionCell.action = actionUrl.toString();

            const button = document.createElement('button');
            button.type = 'submit';
            button.className = 'btn btn-sm btn-outline-danger';
            button.textContent = 'Delete';
            actionCell.appendChild(button);

            header.append(heading, actionCell);

            const meta = document.createElement('div');
            meta.className = 'field-entry-meta mt-3 d-flex flex-wrap gap-2';

            const pageBadge = document.createElement('span');
            pageBadge.className = 'badge rounded-pill text-bg-secondary';
            pageBadge.textContent = `Page ${field.pageNumber}`;
            meta.appendChild(pageBadge);

            const positionBadge = document.createElement('span');
            positionBadge.className = 'badge rounded-pill text-bg-dark';
            positionBadge.textContent = `X ${Number(field.x).toFixed(2)}, Y ${Number(field.y).toFixed(2)}`;
            meta.appendChild(positionBadge);

            const sizeBadge = document.createElement('span');
            sizeBadge.className = 'badge rounded-pill text-bg-dark';
            sizeBadge.textContent = `W ${Number(field.width).toFixed(2)}, H ${Number(field.height).toFixed(2)}`;
            meta.appendChild(sizeBadge);

            entry.append(header, meta);
            rows.appendChild(entry);
        };

        const handleDeleteFieldSubmit = async (form) => {
            if (!form) {
                return;
            }

            const action = form.action || window.location.href;
            const response = await fetch(action, {
                method: 'POST',
                body: new FormData(form),
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    Accept: 'application/json',
                },
                credentials: 'same-origin',
            });

            const contentType = response.headers.get('content-type') || '';
            const payload = contentType.includes('application/json')
                ? await response.json()
                : { message: await response.text() };

            if (!response.ok) {
                throw new Error(payload?.message || 'Unable to delete the field.');
            }

            const deletedFieldId = payload?.deletedFieldId || new URL(action).searchParams.get('signatureFieldId');
            removeFieldById(deletedFieldId);
            setStatus(payload?.message || 'Signature field deleted.');
        };

        rows?.addEventListener('submit', (event) => {
            const form = event.target instanceof HTMLFormElement ? event.target : null;
            if (!form || form.dataset.deleteFieldForm !== 'true') {
                return;
            }

            event.preventDefault();

            if (!window.confirm('Delete this field?')) {
                return;
            }

            handleDeleteFieldSubmit(form).catch((error) => {
                console.error(error);
                setStatus(error instanceof Error ? error.message : 'Unable to delete the field.');
            });
        });

        const handlePointerDown = (event) => {
            if (event.button !== 0) {
                return;
            }

            const point = toCanvasPoint(event.clientX, event.clientY);
            if (!point) {
                return;
            }

            state.drawing = true;
            state.pointerId = event.pointerId;
            state.startPoint = point;
            shell.classList.add('is-drawing-field');

            if (typeof shell.setPointerCapture === 'function') {
                try {
                    shell.setPointerCapture(event.pointerId);
                } catch {
                    // Ignore pointer capture failures and continue with local events.
                }
            }

            const draft = toPdfRectFromCanvasPoints(point, point);
            const inputs = getInputs();
            const width = Number(inputs.width?.value || '180');
            const height = Number(inputs.height?.value || '60');

            setDraft({
                x: draft.x,
                y: draft.y,
                width,
                height,
            });

            event.preventDefault();
        };

        const handlePointerMove = (event) => {
            if (!state.drawing || state.pointerId !== event.pointerId || !state.startPoint) {
                return;
            }

            const point = toCanvasPoint(event.clientX, event.clientY);
            if (!point) {
                return;
            }

            const draft = toPdfRectFromCanvasPoints(state.startPoint, point);
            setDraft(draft);
            event.preventDefault();
        };

        const handlePointerUp = (event) => {
            if (!state.drawing || state.pointerId !== event.pointerId || !state.startPoint) {
                return;
            }

            const point = toCanvasPoint(event.clientX, event.clientY);
            if (!point) {
                state.drawing = false;
                state.startPoint = null;
                state.pointerId = null;
                shell.classList.remove('is-drawing-field');
                return;
            }

            const movedX = Math.abs(point.x - state.startPoint.x);
            const movedY = Math.abs(point.y - state.startPoint.y);
            const minMovement = 4;
            const inputs = getInputs();

            if (movedX < minMovement && movedY < minMovement) {
                const clicked = toPdfRectFromCanvasPoints(point, point);
                const currentWidth = Number(inputs.width?.value || '180');
                const currentHeight = Number(inputs.height?.value || '60');
                setDraft({
                    x: clicked.x,
                    y: clicked.y,
                    width: currentWidth,
                    height: currentHeight,
                });
            } else {
                setDraft(toPdfRectFromCanvasPoints(state.startPoint, point));
            }

            state.drawing = false;
            state.startPoint = null;
            state.pointerId = null;
            shell.classList.remove('is-drawing-field');
            event.preventDefault();
        };

        const handlePointerCancel = () => {
            state.drawing = false;
            state.startPoint = null;
            state.pointerId = null;
            shell.classList.remove('is-drawing-field');
        };

        const updatePageNumber = () => {
            if (!pageInput) {
                return;
            }

            const parsed = Number(pageInput.value);
            if (!Number.isFinite(parsed) || parsed < 1) {
                pageInput.value = String(state.currentPageNumber);
                return;
            }

            state.draft = null;
            renderPage(parsed).catch((error) => {
                console.error(error);
                setStatus('Unable to render that page.');
            });
        };

        const handleFormSubmit = (event) => {
            event.preventDefault();
            submitFieldForm();
        };

        shell.addEventListener('pointerdown', handlePointerDown);
        shell.addEventListener('pointermove', handlePointerMove);
        shell.addEventListener('pointerup', handlePointerUp);
        shell.addEventListener('pointercancel', handlePointerCancel);

        if (pageInput) {
            pageInput.addEventListener('change', updatePageNumber);
        }

        if (addFieldForm) {
            addFieldForm.addEventListener('submit', handleFormSubmit);
        }

        if (typeof ResizeObserver !== 'undefined') {
            const observer = new ResizeObserver(() => {
                renderBoxes();
            });

            observer.observe(shell);
        } else {
            window.addEventListener('resize', () => renderBoxes());
        }

        updateEmptyState();
        updateReadyButton();

        renderPage(state.currentPageNumber).catch((error) => {
            console.error(error);
            setStatus('Unable to load the preview.');
        });
    }
}
