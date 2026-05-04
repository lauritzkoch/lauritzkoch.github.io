document.addEventListener('DOMContentLoaded', function() {
    const downloadBtn = document.getElementById('download-pdf');
    
    if (downloadBtn) {
        downloadBtn.addEventListener('click', function() {
            // Check if html2pdf library is available
            if (typeof html2pdf === 'undefined') {
                alert('PDF library is loading. Please try again in a moment.');
                return;
            }
            
            const element = document.querySelector('.page');
            const opt = {
                margin: [5, 10, 5, 10],
                filename: 'Lauritz_Fokdal_Koch_CV.pdf',
                image: { type: 'jpeg', quality: 0.98 },
                html2canvas: { scale: 2, useCORS: true, allowTaint: true, logging: false },
                jsPDF: { orientation: 'portrait', unit: 'mm', format: 'a4', compress: true },
                pagebreak: { mode: 'css', before: '.content section', avoid: 'article' }
            };
            
            html2pdf().set(opt).from(element).save();
        });
    }
});
