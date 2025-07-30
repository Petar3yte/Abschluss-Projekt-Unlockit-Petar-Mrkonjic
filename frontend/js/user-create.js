document.addEventListener('DOMContentLoaded', function () {
    const token = localStorage.getItem('authToken');
    if (!token) {
        window.location.href = 'login.html';
        return;
    }

    const createUserForm = document.getElementById('create-user-form');
    const errorMessageDiv = document.getElementById('error-message');

    createUserForm.addEventListener('submit', async (event) => {
        event.preventDefault();

        const userData = {
            userName: document.getElementById('username').value,
            email: document.getElementById('email').value,
            password: document.getElementById('password').value,
            firstName: document.getElementById('firstname').value,
            lastName: document.getElementById('lastname').value,
            role: document.getElementById('role-select').value
        };

        try {
            const response = await fetch('https://localhost:7007/api/Users', {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(userData)
            });

            if (response.ok) {
                alert('Benutzer erfolgreich angelegt!');
                window.location.href = 'users.html';
            } else {
                const errorData = await response.json();
                errorMessageDiv.textContent = errorData.message || `Fehler: ${response.statusText}`;
                errorMessageDiv.classList.remove('d-none');
            }

        } catch (error) {
            console.error('Fehler beim Erstellen des Benutzers:', error);
            errorMessageDiv.textContent = 'Ein Netzwerkfehler ist aufgetreten. Bitte erneut versuchen.';
            errorMessageDiv.classList.remove('d-none');
        }
    });
});