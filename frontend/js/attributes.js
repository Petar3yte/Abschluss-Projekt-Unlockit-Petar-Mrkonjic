document.addEventListener('DOMContentLoaded', () => {
    const token = localStorage.getItem('authToken');
    if (!token) { window.location.href = 'login.html'; return; }

    const logoutButton = document.getElementById('logout-button');
    if (logoutButton) {
        logoutButton.addEventListener('click', () => {
            localStorage.removeItem('authToken');
            localStorage.removeItem('currentUser');
            window.location.href = 'login.html';
        });
    }
    setupAttributeSection('platform', 'Platforms', 'Plattform');
    setupAttributeSection('genre', 'Genres', 'Genre');
});

function setupAttributeSection(idPrefix, apiController, name) {
    const list = document.getElementById(`${idPrefix}-list`);
    const form = document.getElementById(`${idPrefix}-form`);
    const token = localStorage.getItem('authToken'); 

    async function loadItems() {
        try {
            const response = await fetch(`https://localhost:7007/api/${apiController}`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!response.ok) throw new Error(`${name}-Liste konnte nicht geladen werden.`);
            
            const items = await response.json();
            list.innerHTML = '';
            items.forEach(item => {
                const li = document.createElement('li');
                li.className = 'list-group-item d-flex justify-content-between align-items-center';
                li.textContent = item.name;
                const deleteBtn = document.createElement('button');
                deleteBtn.className = 'btn btn-sm btn-danger';
                deleteBtn.textContent = 'X';
                deleteBtn.onclick = () => deleteItem(item[`${idPrefix}Id`]);
                li.appendChild(deleteBtn);
                list.appendChild(li);
            });
        } catch(error) {
            console.error(`Fehler beim Laden von ${name}:`, error);
        }
    }

    async function createItem(itemName) {
        try {
            await fetch(`https://localhost:7007/api/${apiController}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
                body: JSON.stringify({ name: itemName })
            });
            await loadItems();
        } catch(error) {
             console.error(`Fehler beim Erstellen von ${name}:`, error);
        }
    }

    async function deleteItem(itemId) {
        if (!confirm(`${name} wirklich löschen?`)) return;
        try {
            await fetch(`https://localhost:7007/api/${apiController}/${itemId}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${token}` }
            });
            await loadItems();
        } catch(error) {
            console.error(`Fehler beim Löschen von ${name}:`, error);
        }
    }

    form.addEventListener('submit', (e) => {
        e.preventDefault();
        const input = form.querySelector('input');
        if (input.value.trim()) {
            createItem(input.value.trim());
            input.value = '';
        }
    });

    loadItems();
}