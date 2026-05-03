const backendUrl = 'http://localhost:5204/job/assistant';
const actionStatus = document.getElementById('action-status');
const outputSection = document.getElementById('output-section');
const outputLabel = document.getElementById('output-label');
const outputText = document.getElementById('output-text');
const outputMeta = document.getElementById('output-meta');
const matchedSkills = document.getElementById('matched-skills');
const matchedSkillsList = document.getElementById('matched-skills-list');
const potentialGaps = document.getElementById('potential-gaps');
const potentialGapsList = document.getElementById('potential-gaps-list');

const actionLabels = {
    'analyze': 'Job Posting Analysis',
    'application': 'Application Draft',
    'cv-summary': 'Tailored CV Summary',
    'cv-bullets': 'CV Bullet Suggestions',
    'interview-prep': 'Interview Preparation Notes'
};

function getInputs() {
    return {
        jobLink: document.getElementById('job-link').value.trim(),
        companyLink: document.getElementById('company-link').value.trim() || null,
        language: document.getElementById('language').value,
        tone: document.getElementById('tone').value,
        notes: document.getElementById('notes').value.trim() || null,
        recommendation: document.getElementById('recommendation').value.trim() || null
    };
}

function showStatus(message, isError) {
    actionStatus.textContent = message;
    actionStatus.classList.remove('is-hidden', 'error');
    if (isError) actionStatus.classList.add('error');
}

function hideStatus() {
    actionStatus.classList.add('is-hidden');
}

function setButtonsDisabled(disabled) {
    document.querySelectorAll('.action-btn').forEach(function (btn) {
        btn.disabled = disabled;
    });
}

function renderOutput(label, content, meta) {
    outputLabel.textContent = label;
    outputText.value = content;
    outputSection.classList.remove('is-hidden');

    if (meta && meta.matchedSkills && meta.matchedSkills.length > 0) {
        matchedSkillsList.innerHTML = meta.matchedSkills.map(function (s) { return '<li>' + s + '</li>'; }).join('');
        matchedSkills.classList.remove('is-hidden');
        outputMeta.classList.remove('is-hidden');
    } else {
        matchedSkills.classList.add('is-hidden');
    }

    if (meta && meta.potentialGaps && meta.potentialGaps.length > 0) {
        potentialGapsList.innerHTML = meta.potentialGaps.map(function (s) { return '<li>' + s + '</li>'; }).join('');
        potentialGaps.classList.remove('is-hidden');
        outputMeta.classList.remove('is-hidden');
    } else {
        potentialGaps.classList.add('is-hidden');
    }

    var hasSkills = meta && meta.matchedSkills && meta.matchedSkills.length > 0;
    var hasGaps = meta && meta.potentialGaps && meta.potentialGaps.length > 0;
    if (!hasSkills && !hasGaps) {
        outputMeta.classList.add('is-hidden');
    }

    outputText.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

async function callAssistant(action) {
    var inputs = getInputs();
    if (!inputs.jobLink) {
        showStatus('Please enter a job posting URL.', true);
        return;
    }

    setButtonsDisabled(true);
    showStatus('Generating ' + actionLabels[action] + '…  This may take 15-30 seconds.', false);

    try {
        var response = await fetch(backendUrl + '/' + action, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(inputs)
        });

        if (!response.ok) {
            var errorText = await response.text();
            showStatus('Error: ' + errorText, true);
            return;
        }

        var result = await response.json();
        hideStatus();
        renderOutput(
            actionLabels[action],
            result.content || '',
            { matchedSkills: result.matchedSkills, potentialGaps: result.potentialGaps }
        );
    } catch (error) {
        showStatus('Failed to connect to backend. Make sure it is running at ' + backendUrl, true);
    } finally {
        setButtonsDisabled(false);
    }
}

document.querySelectorAll('.action-btn').forEach(function (btn) {
    btn.addEventListener('click', function () {
        callAssistant(btn.dataset.action);
    });
});

document.getElementById('btn-copy').addEventListener('click', function () {
    outputText.select();
    navigator.clipboard.writeText(outputText.value).then(function () {
        var btn = document.getElementById('btn-copy');
        btn.textContent = 'Copied!';
        setTimeout(function () { btn.textContent = 'Copy to clipboard'; }, 2000);
    });
});

document.getElementById('btn-save').addEventListener('click', function () {
    var inputs = getInputs();
    var draft = {
        jobLink: inputs.jobLink,
        companyLink: inputs.companyLink,
        notes: outputText.value,
        language: inputs.language,
        tone: inputs.tone,
        label: outputLabel.textContent
    };
    localStorage.setItem('assistantDraft', JSON.stringify(draft));
    var params = new URLSearchParams();
    if (inputs.jobLink) params.set('jobLink', inputs.jobLink);
    params.set('fromAssistant', '1');
    window.location.href = 'job-applications.html?' + params.toString();
});

document.addEventListener('DOMContentLoaded', function () {
    var urlParams = new URLSearchParams(window.location.search);
    var jobLinkParam = urlParams.get('jobLink');
    if (jobLinkParam) {
        document.getElementById('job-link').value = decodeURIComponent(jobLinkParam);
    }
});
