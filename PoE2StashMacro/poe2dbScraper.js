// Function to scrape the data
function scrapeData() {
    // Initialize the result object
    const result = {
        type: "",
        name: "",
        baseType: "",
        stat: [],
        implicitStatsToMatch: [],
        augmentedStatsToMatch: [],
        enabled: true
    };

    // Upper left title of the item via XPath
    const typeElement = document.evaluate('//*[@id="canvas"]/h5/a', document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
    if (typeElement) {
        result.type = typeElement.innerText.trim();
        result.name = result.type + " (first)";
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
                const tdRows = row.querySelectorAll('td');
                const name = tdRows[0].textContent.trim();

                const descriptionRaw = tdRows[2];
                const spans = descriptionRaw.querySelectorAll('span');
                let targetIndex = -1;
                for (let i = 0; i < spans.length; i++) {
                    if (spans[i].classList.contains('d-none') &&
                        spans[i].classList.contains('badge') &&
                        spans[i].classList.contains('rounded-pill') &&
                        spans[i].classList.contains('bg-secondary')) {
                        targetIndex = i;
                        break;
                    }
                }
                for (let i = 0; i < Math.min(targetIndex + 1, spans.length); i++) { // remove first 2 OR min length
                    spans[i].remove();
                }
                for (let i = 0; i < spans.length; i++) {
                    if (spans[i].classList.contains('mod-value')) {
                        // Modify the textContent to prepend a newline character
                        spans[i].textContent = "\n" + spans[i].textContent;

                        // Clean up the text by removing "\n" before "("
                        spans[i].textContent = spans[i].textContent.replace(/\n\(/g, "(");
                    }
                }

                const description = descriptionRaw.textContent.trim();

                // Push the stat object to the interim result
                interimResult.push({
                    name: name,
                    description: description,
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