const backendUrl = 'http://localhost:5204/job';
        const storageKey = 'jobApplications';
        const form = document.getElementById('application-form');
        const statusMessage = document.getElementById('status-message');
        const loadLinkMessage = document.getElementById('load-link-message');
        const addJobLinkInput = document.getElementById('add-job-link');
        const loadJobLinkButton = document.getElementById('load-job-link');
        const applicationsBody = document.getElementById('applications-body');
        const clearButton = document.getElementById('clear-storage');
        const cancelEditButton = document.getElementById('cancel-edit');
        const syncStatus = document.getElementById('sync-status');
        const filterStatus = document.getElementById('filter-status');
        let editingId = null;

        function createId() {
            return 'id-' + Math.random().toString(36).slice(2) + Date.now();
        }

        function loadLocalApplications() {
            try {
                return JSON.parse(localStorage.getItem(storageKey) || '[]');
            } catch {
                return [];
            }
        }

        function saveLocalApplications(items) {
            localStorage.setItem(storageKey, JSON.stringify(items));
        }

        function setSyncStatus(message, isError = false) {
            syncStatus.textContent = message;
            syncStatus.style.color = isError ? '#7a1f1f' : '#0f3f6f';
        }

        function formatDate(value) {
            if (!value) return '-';
            const date = new Date(value);
            if (Number.isNaN(date.getTime())) return '-';
            return date.toLocaleDateString();
        }

        function showMessage(text, success = true) {
            statusMessage.textContent = text;
            statusMessage.classList.remove('is-hidden');
            statusMessage.style.backgroundColor = success ? '#eef7ff' : '#ffe6e6';
            statusMessage.style.color = success ? '#0f3f6f' : '#7a1f1f';
            setTimeout(() => {
                statusMessage.classList.add('is-hidden');
            }, 6000);
        }

        function setEditMode(isEditing) {
            const jobLinkField = document.getElementById('jobLink');
            jobLinkField.disabled = isEditing;
            loadJobLinkButton.disabled = isEditing;
            addJobLinkInput.disabled = isEditing;
            const submitButton = form.querySelector('button[type="submit"]');
            submitButton.textContent = isEditing ? 'Update application' : 'Save application';
            if (!isEditing) {
                loadLinkMessage.classList.add('is-hidden');
            }
        }

        async function loadJobDetailsFromLink() {
            const link = addJobLinkInput.value.trim();
            if (!link) {
                loadLinkMessage.textContent = 'Please enter a job link to load.';
                loadLinkMessage.classList.remove('is-hidden');
                loadLinkMessage.style.backgroundColor = '#ffe6e6';
                loadLinkMessage.style.color = '#7a1f1f';
                return;
            }

            try {
                const response = await fetch(`${backendUrl}/process`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ jobLink: link })
                });

                if (!response.ok) {
                    const payload = await response.text();
                    loadLinkMessage.textContent = `Unable to load job: ${payload}`;
                    loadLinkMessage.classList.remove('is-hidden');
                    loadLinkMessage.style.backgroundColor = '#ffe6e6';
                    loadLinkMessage.style.color = '#7a1f1f';
                    return;
                }

                const result = await response.json();
                const application = result.application || result.Application || {};
                document.getElementById('jobLink').value = application.jobLink || application.JobLink || link;
                document.getElementById('role').value = application.role || application.Role || '';
                document.getElementById('company').value = application.company || application.Company || '';
                document.getElementById('source').value = application.source || application.Source || '';
                document.getElementById('status').value = application.status || application.Status || 'New';
                document.getElementById('nextStep').value = application.nextStep || application.NextStep || '';
                document.getElementById('deadline').value = application.deadline || application.Deadline || '';
                document.getElementById('resumeVersion').value = application.resumeVersion || application.ResumeVersion || '';
                document.getElementById('coverLetterVersion').value = application.coverLetterVersion || application.CoverLetterVersion || '';
                document.getElementById('notes').value = application.notes || application.Notes || '';
                document.getElementById('application-id').value = '';
                editingId = null;
                setEditMode(false);

                loadLinkMessage.textContent = 'Job details loaded. Complete the application and save.';
                loadLinkMessage.classList.remove('is-hidden');
                loadLinkMessage.style.backgroundColor = '#eef7ff';
                loadLinkMessage.style.color = '#0f3f6f';
            } catch {
                loadLinkMessage.textContent = 'Unable to load job details. Check the link and backend availability.';
                loadLinkMessage.classList.remove('is-hidden');
                loadLinkMessage.style.backgroundColor = '#ffe6e6';
                loadLinkMessage.style.color = '#7a1f1f';
            }
        }

        function getFilteredApplications(applications) {
            const filter = filterStatus.value;
            return applications.filter(application => !filter || application.status === filter);
        }

        function renderApplications(applications) {
            const rows = getFilteredApplications(applications)
                .sort((a, b) => new Date(b.createdAt || b.CreatedAtUtc || 0) - new Date(a.createdAt || a.CreatedAtUtc || 0))
                .map(application => {
                    const deadline = application.deadline || application.Deadline || '';
                    const extractedDeadline = application.extractedApplicationDeadline || application.ExtractedApplicationDeadline || '';
                    const id = application.id || application.Id;
                    return `
                        <tr>
                            <td>${application.role || application.Role || '-'}</td>
                            <td>${application.company || application.Company || '-'}</td>
                            <td>${application.extractedJobTitle || application.ExtractedJobTitle || '-'}</td>
                            <td>${application.extractedCompanyName || application.ExtractedCompanyName || '-'}</td>
                            <td>${application.extractedCompanyLink || application.ExtractedCompanyLink ? `<a href="${application.extractedCompanyLink || application.ExtractedCompanyLink}" target="_blank" rel="noreferrer">Link</a>` : '-'}</td>
                            <td>${formatDate(extractedDeadline)}</td>
                            <td>${application.status || application.Status || '-'}</td>
                            <td>${application.nextStep || application.NextStep || '-'}</td>
                            <td>${formatDate(deadline)}</td>
                            <td><a href="${application.jobLink || application.JobLink || '#'}" target="_blank" rel="noreferrer">Open</a></td>
                            <td>
                                <button type="button" class="secondary" data-action="edit" data-id="${id}">Edit</button>
                                <button type="button" class="secondary" data-action="delete" data-id="${id}">Delete</button>
                            </td>
                        </tr>
                    `;
                })
                .join('');
            applicationsBody.innerHTML = rows || '<tr><td colspan="11">No applications available.</td></tr>';
        }

        async function fetchBackendApplications() {
            try {
                const response = await fetch(`${backendUrl}/list`);
                if (!response.ok) return null;
                const items = await response.json();
                return Array.isArray(items) ? items : null;
            } catch {
                return null;
            }
        }

        async function syncApplication(application, isUpdate = false) {
            try {
                const method = isUpdate ? 'PUT' : 'POST';
                const url = isUpdate ? `${backendUrl}/${application.id}` : `${backendUrl}/process`;
                const response = await fetch(url, {
                    method,
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(application)
                });
                if (!response.ok) {
                    const payload = await response.text();
                    showMessage(`Backend error: ${payload}`, false);
                    setSyncStatus('Backend save failed. Using local copy.', true);
                    return false;
                }
                setSyncStatus('Backend sync successful.');
                return true;
            } catch {
                setSyncStatus('Backend unavailable. Changes saved locally.', true);
                return false;
            }
        }

        async function deleteBackendApplication(id) {
            try {
                const response = await fetch(`${backendUrl}/${id}`, { method: 'DELETE' });
                if (!response.ok) {
                    const payload = await response.text();
                    showMessage(`Backend delete error: ${payload}`, false);
                    return false;
                }
                setSyncStatus('Backend delete successful.');
                return true;
            } catch {
                setSyncStatus('Backend unavailable. Delete persisted locally.', true);
                return false;
            }
        }

        function fillForm(application) {
            editingId = application.id || application.Id;
            document.getElementById('application-id').value = editingId;
            document.getElementById('jobLink').value = application.jobLink || application.JobLink || '';
            document.getElementById('role').value = application.role || application.Role || '';
            document.getElementById('company').value = application.company || application.Company || '';
            document.getElementById('source').value = application.source || application.Source || '';
            document.getElementById('status').value = application.status || application.Status || 'New';
            document.getElementById('nextStep').value = application.nextStep || application.NextStep || '';
            document.getElementById('deadline').value = application.deadline || application.Deadline || '';
            document.getElementById('resumeVersion').value = application.resumeVersion || application.ResumeVersion || '';
            document.getElementById('coverLetterVersion').value = application.coverLetterVersion || application.CoverLetterVersion || '';
            document.getElementById('notes').value = application.notes || application.Notes || '';
            cancelEditButton.classList.remove('is-hidden');
            setEditMode(true);
        }

        function clearForm() {
            form.reset();
            editingId = null;
            document.getElementById('application-id').value = '';
            cancelEditButton.classList.add('is-hidden');
            setEditMode(false);
        }

        function getFormData() {
            return {
                id: editingId || createId(),
                jobLink: document.getElementById('jobLink').value.trim(),
                role: document.getElementById('role').value.trim(),
                company: document.getElementById('company').value.trim(),
                source: document.getElementById('source').value.trim(),
                status: document.getElementById('status').value,
                nextStep: document.getElementById('nextStep').value.trim(),
                deadline: document.getElementById('deadline').value,
                notes: document.getElementById('notes').value.trim(),
                resumeVersion: document.getElementById('resumeVersion').value.trim(),
                coverLetterVersion: document.getElementById('coverLetterVersion').value.trim(),
                createdAt: editingId ? null : new Date().toISOString(),
                processedAt: new Date().toISOString()
            };
        }

        form.addEventListener('submit', async event => {
            event.preventDefault();
            const application = getFormData();
            const applications = loadLocalApplications();
            const index = applications.findIndex(item => item.id === application.id || item.Id === application.id);
            const isUpdate = index !== -1;
            if (isUpdate) {
                applications[index] = { ...applications[index], ...application };
            } else {
                applications.push(application);
            }
            saveLocalApplications(applications);
            renderApplications(applications);
            clearForm();
            const synced = await syncApplication(application, isUpdate);
            if (synced) {
                showMessage(isUpdate ? 'Application updated.' : 'Application saved.');
            } else {
                showMessage(isUpdate ? 'Updated locally. Backend sync failed.' : 'Saved locally. Backend sync failed.', false);
            }
        });

        cancelEditButton.addEventListener('click', () => {
            clearForm();
            showMessage('Edit cancelled.', true);
        });

        clearButton.addEventListener('click', () => {
            if (confirm('Clear all saved job applications from local storage?')) {
                localStorage.removeItem(storageKey);
                renderApplications([]);
                setSyncStatus('Local list cleared. Backend still available.', false);
            }
        });

        loadJobLinkButton.addEventListener('click', loadJobDetailsFromLink);

        applicationsBody.addEventListener('click', async event => {
            const button = event.target.closest('button[data-action]');
            if (!button) return;
            const id = button.dataset.id;
            const action = button.dataset.action;
            const applications = loadLocalApplications();
            const index = applications.findIndex(item => item.id === id || item.Id === id);
            if (index === -1) {
                showMessage('Application not found.', false);
                return;
            }
            if (action === 'edit') {
                fillForm(applications[index]);
                window.scrollTo({ top: 0, behavior: 'smooth' });
            }
            if (action === 'delete') {
                if (!confirm('Delete this application?')) return;
                applications.splice(index, 1);
                saveLocalApplications(applications);
                renderApplications(applications);
                const deleted = await deleteBackendApplication(id);
                if (deleted) {
                    showMessage('Application deleted.');
                } else {
                    showMessage('Deleted locally. Backend delete failed.', false);
                }
            }
        });

        filterStatus.addEventListener('change', () => renderApplications(loadLocalApplications()));

        document.addEventListener('DOMContentLoaded', async () => {
            // Check for jobLink query parameter and pre-fill the add link input
            const urlParams = new URLSearchParams(window.location.search);
            const jobLinkParam = urlParams.get('jobLink');
            if (jobLinkParam) {
                document.getElementById('add-job-link').value = decodeURIComponent(jobLinkParam);
            }

            // Check for assistant draft passed via localStorage
            const fromAssistant = urlParams.get('fromAssistant');
            if (fromAssistant) {
                try {
                    const draft = JSON.parse(localStorage.getItem('assistantDraft'));
                    if (draft) {
                        if (draft.jobLink) {
                            document.getElementById('jobLink').value = draft.jobLink;
                            document.getElementById('jobLink').disabled = false;
                        }
                        if (draft.notes) {
                            document.getElementById('notes').value = draft.notes;
                        }
                        if (draft.label) {
                            document.getElementById('coverLetterVersion').value = draft.label;
                        }
                        document.getElementById('status').value = 'New';
                        localStorage.removeItem('assistantDraft');
                        showMessage('Draft loaded from Application Assistant. Review and save.');
                    }
                } catch { }
            }

            const backendApplications = await fetchBackendApplications();
            if (backendApplications) {
                const normalized = backendApplications.map(app => ({ ...app, id: app.id || app.Id }));
                saveLocalApplications(normalized);
                setSyncStatus('Backend connected. Showing saved applications.');
                renderApplications(normalized);
            } else {
                setSyncStatus('Backend offline. Showing local applications.', true);
                renderApplications(loadLocalApplications());
            }
        });
