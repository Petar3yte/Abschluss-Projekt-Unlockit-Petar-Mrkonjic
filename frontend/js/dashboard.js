document.addEventListener('DOMContentLoaded', function () {
    
    const token = localStorage.getItem('authToken');
    if (!token) {
        window.location.href = 'admin-login.html';
        return; 
    }
    const currentUserString = localStorage.getItem('currentUser');
    if (!currentUserString) {
        localStorage.removeItem('authToken');
        window.location.href = 'admin-login.html';
        return;
    }

    try {
        const currentUser = JSON.parse(currentUserString);
        const userRole = currentUser.role;
    
        if (userRole === 'Kunde') {
            alert('Sie haben keine Berechtigung für den Zugriff auf das Admin-Dashboard.');
            localStorage.removeItem('authToken');
            localStorage.removeItem('currentUser');
            window.location.href = 'admin-login.html';
            return;
        }
    
        const billingCard = document.getElementById('billing-card');
        const usersCard = document.getElementById('users-card');
        const paymentMethodsCard = document.getElementById('payment-methods-card');
        
        const billingNavLink = document.getElementById('billing-nav-link');
        const usersNavLink = document.getElementById('users-nav-link');
        const paymentMethodsNavLink = document.getElementById('payment-methods-nav-link');
        
        const categoriesNavLink = document.getElementById('categories-nav-link'); 
        const attributesNavLink = document.getElementById('attributes-nav-link'); 
    
        // Logik für "Mitarbeiter"
        if (userRole === 'Mitarbeiter') {
            if (billingCard) billingCard.style.display = 'none';
            if (usersCard) usersCard.style.display = 'none';
            if (paymentMethodsCard) paymentMethodsCard.style.display = 'none';

            if (billingNavLink) billingNavLink.style.display = 'none';
            if (usersNavLink) usersNavLink.style.display = 'none';
            if (paymentMethodsNavLink) paymentMethodsNavLink.style.display = 'none';

            if (categoriesNavLink) categoriesNavLink.style.display = 'none';
            if (attributesNavLink) attributesNavLink.style.display = 'none';
        }
    
    } catch (error) {
        console.error('Fehler beim Verarbeiten der Benutzerdaten:', error);
        localStorage.removeItem('authToken');
        localStorage.removeItem('currentUser');
        window.location.href = 'admin-login.html';
        return;
    }

    const logoutButton = document.getElementById('logout-button');
    if (logoutButton) {
        logoutButton.addEventListener('click', function() {
            localStorage.removeItem('authToken');
            localStorage.removeItem('currentUser');
            
            window.location.href = 'admin-login.html';
        });
    }    
});