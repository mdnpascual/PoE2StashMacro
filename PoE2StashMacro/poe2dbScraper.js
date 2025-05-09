// Function to scrape the data
function scrapeData() {
    // Initialize the result object
    const result = {
        type: "",
        stat: [],
        implicitStatsToMatch: ["IGNORE_FOR_NOW"],
        enabled: true
    };

    // Upper left title of the item via XPath
    const typeElement = document.evaluate('//*[@id="canvas"]/h5/a', document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
    if (typeElement) {
        result.type = typeElement.innerText.trim();
    }

    // Get all divs that start with "collapseOnenormal"
    const statDivs = document.querySelectorAll('div[id^="collapseOnenormal"]');

    statDivs.forEach(div => {
        // Find the tables within the modal body
        const tables = div.querySelectorAll('.modal-body .table');

        tables.forEach(table => {
            const rows = table.querySelectorAll('tbody tr');
            let interimResult = [];

            // Collect name and tier
            rows.forEach((row, index) => {
                const name = row.querySelector('td').textContent.trim();
                // Push the stat object to the interim result
                interimResult.push({
                    name: name,
                    tier: index + 1,
                    rank: index + 1
                });
            });

            // Ranks are flipped tier
            const highestRank = interimResult.length;
            interimResult.forEach((stat, index) => {
                const rank = highestRank - index; // Reverse the rank
                stat.rank = rank; // Assign the flipped rank
                stat.enabled = (rank === 1); // Set enabled only for rank 1
            });

            // Push the modified interim results to the main result
            result.stat.push(...interimResult);
        });
    });

    console.log(JSON.stringify(result, null, 2));
}

scrapeData();