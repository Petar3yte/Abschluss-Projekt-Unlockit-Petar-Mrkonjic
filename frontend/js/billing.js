document.addEventListener('DOMContentLoaded', function () {
    const token = localStorage.getItem('authToken');
    const currentUserString = localStorage.getItem('currentUser');

    if (!token || !currentUserString) {
        window.location.href = 'login.html';
        return;
    }

    try {
        const user = JSON.parse(currentUserString);
        if (user.role !== 'Admin') {
            alert('Zugriff verweigert. Nur Administratoren dürfen auf die Abrechnung zugreifen.');
            window.location.href = 'dashboard.html';
            return;
        }
    } catch (error) {
        console.error("Fehler beim Parsen der Benutzerdaten:", error);
        window.location.href = 'login.html';
        return;
    }

    const yearSelect = document.getElementById('year-select');
    const monthSelect = document.getElementById('month-select');
    const fetchBtn = document.getElementById('fetch-billing-btn');
    const monthlyTransactionsBody = document.getElementById('monthly-transactions-body');
    const allTimeTransactionsBody = document.getElementById('all-time-transactions-body');

    function populateSelectors() {
        const currentYear = new Date().getFullYear();       
        for (let i = 0; i < 5; i++) {
            const year = currentYear - i;
            const option = new Option(year, year);
            yearSelect.add(option);
        }

        const months = ["Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember"];
        months.forEach((month, index) => {
            const option = new Option(month, index + 1);
            monthSelect.add(option);
        });

        monthSelect.value = new Date().getMonth() + 1;
        yearSelect.value = currentYear;
    }

    async function fetchBillingData() {
        const year = yearSelect.value;
        const month = monthSelect.value;
        await fetchMonthlySummary(year, month);
        await fetchMonthlyTransactions(year, month);
    }

    async function fetchMonthlySummary(year, month) {
        const monthName = monthSelect.options[monthSelect.selectedIndex].text;
        try {
            const response = await fetch(`https://localhost:7007/api/billing/summary?year=${year}&month=${month}`, {
                method: 'GET',
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!response.ok) throw new Error(`HTTP-Fehler! Status: ${response.status}`);
            const summary = await response.json();
            updateUI(summary, monthName);
        } catch (error) {
            console.error('Fehler beim Abrufen der monatlichen Abrechnungsdaten:', error);
        }
    }

    async function fetchMonthlyTransactions(year, month) {
        try {
            const response = await fetch(`https://localhost:7007/api/billing/transactions?year=${year}&month=${month}`, {
                method: 'GET',
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!response.ok) throw new Error(`HTTP-Fehler! Status: ${response.status}`);
            const transactions = await response.json();
            displayTransactions(transactions, monthlyTransactionsBody);
        } catch (error) {
            console.error('Fehler beim Abrufen der monatlichen Transaktionen:', error);
            monthlyTransactionsBody.innerHTML = `<tr><td colspan="4" class="text-danger">Fehler beim Laden der Transaktionen.</td></tr>`;
        }
    }

    async function fetchAllTimeTransactions() {
        try {
            const response = await fetch(`https://localhost:7007/api/billing/transactions/all`, {
                method: 'GET',
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!response.ok) throw new Error(`HTTP-Fehler! Status: ${response.status}`);
            const transactions = await response.json();
            displayTransactions(transactions, allTimeTransactionsBody);
        } catch (error) {
            console.error('Fehler beim Abrufen aller Transaktionen:', error);
            allTimeTransactionsBody.innerHTML = `<tr><td colspan="4" class="text-danger">Fehler beim Laden der Transaktionen.</td></tr>`;
        }
    }

    function displayTransactions(transactions, tableBody) {
        tableBody.innerHTML = '';
        if (transactions.length === 0) {
            tableBody.innerHTML = `<tr><td colspan="4" class="text-center">Keine Transaktionen in diesem Zeitraum.</td></tr>`;
            return;
        }

        transactions.forEach(tx => {
            const row = tableBody.insertRow();
            const typeClass = tx.type === 'Einnahme' ? 'transaction-income' : 'transaction-expense';
            const amountPrefix = tx.type === 'Einnahme' ? '+' : '-';

            row.innerHTML = `
                <td>${new Date(tx.transactionDate).toLocaleDateString('de-DE')}</td>
                <td class="${typeClass}">${tx.type}</td>
                <td>${tx.description}</td>
                <td class="text-end ${typeClass}">${amountPrefix}${new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' }).format(tx.amount)}</td>
            `;
        });
    }

    function updateUI(summary, monthName) {
        const formatCurrency = (value) => new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' }).format(value);
        const netResult = summary.totalIncome - summary.totalExpenses;

        document.getElementById('result-period').textContent = `${monthName} ${summary.year}`;
        document.getElementById('total-income').textContent = formatCurrency(summary.totalIncome);
        document.getElementById('total-expenses').textContent = formatCurrency(summary.totalExpenses);
        document.getElementById('net-result').textContent = formatCurrency(netResult);

        const netResultElement = document.getElementById('net-result');
        netResultElement.classList.remove('text-success', 'text-danger', 'text-muted');
        if (netResult > 0) netResultElement.classList.add('text-success');
        else if (netResult < 0) netResultElement.classList.add('text-danger');
        else netResultElement.classList.add('text-muted');
    }
    
    async function fetchOverallBillingData() {
        try {
            const response = await fetch(`https://localhost:7007/api/billing/summary/overall`, {
                method: 'GET',
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!response.ok) throw new Error(`HTTP-Fehler! Status: ${response.status}`);
            const summary = await response.json();
            updateOverallUI(summary);
        } catch (error) {
            console.error('Fehler beim Abrufen der Gesamt-Abrechnungsdaten:', error);
        }
    }
    
    function updateOverallUI(summary) {
        const formatCurrency = (value) => new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' }).format(value);
        const netResult = summary.totalIncome - summary.totalExpenses;
    
        document.getElementById('overall-income').textContent = formatCurrency(summary.totalIncome);
        document.getElementById('overall-expenses').textContent = formatCurrency(summary.totalExpenses);
        document.getElementById('overall-net-result').textContent = formatCurrency(netResult);
    
        const netResultElement = document.getElementById('overall-net-result');
        netResultElement.classList.remove('text-success', 'text-danger', 'text-muted');
        if (netResult > 0) netResultElement.classList.add('text-success');
        else if (netResult < 0) netResultElement.classList.add('text-danger');
        else netResultElement.classList.add('text-muted');
    }

    populateSelectors();
    fetchBtn.addEventListener('click', fetchBillingData);    
    
    // Initial fetch
    fetchBillingData();
    fetchOverallBillingData();
    fetchAllTimeTransactions();
});