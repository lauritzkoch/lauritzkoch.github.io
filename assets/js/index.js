document.getElementById('process-btn').addEventListener('click', () => {
            const jobLink = document.getElementById('job-link').value;
            if (!jobLink) {
                alert('Please enter a job link.');
                return;
            }

            // Encode the job link and navigate to job-applications.html
            const encodedLink = encodeURIComponent(jobLink);
            window.location.href = `job-applications.html?jobLink=${encodedLink}`;
        });

        document.getElementById('test-btn').addEventListener('click', async () => {
            try {
                const response = await fetch('http://localhost:5204/job/test');

                if (response.ok) {
                    const result = await response.json();
                    alert('Backend test: ' + result.message);
                } else {
                    alert('Backend test failed.');
                }
            } catch (error) {
                alert('Failed to connect to backend test endpoint.');
            }
        });
