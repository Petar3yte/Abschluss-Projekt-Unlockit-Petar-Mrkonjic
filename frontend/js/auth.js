document.addEventListener('DOMContentLoaded', () => {
    const loginForm = document.getElementById('login-form');
    const errorMessageDiv = document.getElementById('error-message');

    loginForm.addEventListener('submit', async (event) => {
        event.preventDefault(); 
        
        errorMessageDiv.classList.add('d-none');
        errorMessageDiv.textContent = '';

        const username = document.getElementById('username').value;
        const password = document.getElementById('password').value;

        const loginData = {
            userName: username,
            password: password
        };

        try {
            const response = await fetch('https://localhost:7007/api/Auth/login', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(loginData)
            });

            if (response.ok) {
                const result = await response.json(); 
                
                localStorage.setItem('authToken', result.token);
                localStorage.setItem('currentUser', JSON.stringify(result.user));

                window.location.href = 'dashboard.html'; 
            } else {
                const errorData = await response.json();
                const message = errorData.message || 'Anmeldung fehlgeschlagen. Bitte überprüfen Sie Ihre Eingaben.';
                errorMessageDiv.textContent = message;
                errorMessageDiv.classList.remove('d-none'); 
            }
        } catch (error) {
            console.error('Login-Fehler:', error);
            errorMessageDiv.textContent = 'Ein Verbindungsfehler ist aufgetreten. Läuft das Backend?';
            errorMessageDiv.classList.remove('d-none');
        }
    });
});