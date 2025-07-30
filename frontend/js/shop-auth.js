document.addEventListener('DOMContentLoaded', () => {
    const loginForm = document.getElementById('shop-login-form');
    const registerForm = document.getElementById('shop-register-form');
    const errorMessageDiv = document.getElementById('error-message');

    if (loginForm) {
        loginForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            errorMessageDiv.textContent = ''; 

            const username = document.getElementById('username').value;
            const password = document.getElementById('password').value;

            try {
                const response = await fetch('https://localhost:7007/api/auth/login', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ username, password })
                });

                if (!response.ok) {
                    const errorData = await response.json();
                    throw new Error(errorData.message || 'Login fehlgeschlagen.');
                }

                const data = await response.json();
                localStorage.setItem('authToken', data.token);
                localStorage.setItem('currentUser', JSON.stringify(data.user));

                await mergeAndSyncCart(data.token);

                window.location.href = 'index.html';

            } catch (error) {
                errorMessageDiv.textContent = error.message;
            }
        });
    }

    async function mergeAndSyncCart(token) {
        const localCart = JSON.parse(localStorage.getItem('unlockitCart')) || [];
        
        if (localCart.length > 0) {
            try {
                const mergeResponse = await fetch('https://localhost:7007/api/Cart/merge', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${token}`
                    },
                    body: JSON.stringify(localCart)
                });
                if (!mergeResponse.ok) throw new Error("Fehler beim Zusammenführen des Warenkorbs");

                // Aktualisierten Warenkorb vom Server holen und lokal speichern
                const updatedCartItems = await mergeResponse.json();
                const updatedLocalCart = updatedCartItems.map(item => ({ uuid: item.productUuid, quantity: item.quantity }));
                localStorage.setItem('unlockitCart', JSON.stringify(updatedLocalCart));

            } catch (error) {
                console.error(error);
                // Wenn das Zusammenführen fehlschlägt, leeren wir den lokalen Warenkorb nicht
            }
        } else {
            // Wenn der lokale Warenkorb leer ist, holen wir einfach den Server-Warenkorb
            if(window.syncCartWithServer) {
                await window.syncCartWithServer();
            }
        }
    }


    if (registerForm) {
        registerForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            errorMessageDiv.textContent = '';

            const email = document.getElementById('email').value;
            const username = document.getElementById('username').value;
            const password = document.getElementById('password').value;
            
            const role = 'Kunde';

            try {
                const response = await fetch('https://localhost:7007/api/auth/register', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ email, username, password, role })
                });

                if (!response.ok) {
                    const errorData = await response.json();
                    throw new Error(errorData.message || 'Registrierung fehlgeschlagen.');
                }
                
                alert('Registrierung erfolgreich! Du kannst dich jetzt anmelden.');
                window.location.href = 'shop-login.html';

            } catch (error) {
                errorMessageDiv.textContent = error.message;
            }
        });
    }
});