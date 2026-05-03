document.addEventListener('DOMContentLoaded', function() {
    // Determine the correct path based on whether we're using file:// or http://
    let headerPath = 'assets/html/header.html';
    
    // If running via file:// protocol, construct absolute path
    if (window.location.protocol === 'file:') {
        const currentDir = window.location.pathname.substring(0, window.location.pathname.lastIndexOf('/'));
        headerPath = currentDir + '/assets/html/header.html';
    }
    
    fetch(headerPath)
        .then(response => {
            if (!response.ok) throw new Error('Header not found');
            return response.text();
        })
        .then(html => {
            const header = document.createElement('div');
            header.innerHTML = html;
            document.body.insertBefore(header.firstElementChild, document.body.firstChild);
            
            // Add header CSS if not already loaded
            if (!document.querySelector('link[href*="header.css"]')) {
                const link = document.createElement('link');
                link.rel = 'stylesheet';
                link.href = 'assets/css/header.css';
                document.head.appendChild(link);
            }
            
            // Highlight current page link
            const currentPage = window.location.pathname.split('/').pop() || 'index.html';
            document.querySelectorAll('.nav-link').forEach(link => {
                const href = link.getAttribute('href');
                if (href === currentPage || (currentPage === '' && href === 'index.html')) {
                    link.style.color = '#4f7ba7';
                    link.style.backgroundColor = 'rgba(79, 123, 167, 0.12)';
                    link.style.borderColor = 'rgba(79, 123, 167, 0.25)';
                }
            });
        })
        .catch(error => console.warn('Header not loaded (expected when opening files locally):', error));
});
