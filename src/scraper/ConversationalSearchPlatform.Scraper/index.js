const express = require('express');
const puppeteer = require('puppeteer');
const app = express();

async function scrapeUrl(url) {
    let content = undefined;
    let browser = undefined;
    try {
        const args = ["--disable-gpu", "--disable-dev-shm-usage", "--disable-setuid-sandbox", "--no-sandbox"]
        browser = await puppeteer.launch({headless: 'new', args: args});
        console.log("Started browser")
        const page = await browser.newPage();
        await page.setJavaScriptEnabled(true);
        console.log("Going to page")

        await page.goto(url, {waitUntil: 'networkidle2'});
        console.log("Waiting for page")
        await page.waitForNetworkIdle({idleTime: 3000})
        console.log("Getting content for page")

        content = await page.content();
    } catch (e) {
        console.error(e);
    } finally {
        if (browser) {
            browser.close();
        }
    }
    return content;
}

app.get('/scrape', async function (req, res) {
    const url = req.query.url;
    console.log(`Got ${url} to scrape`);
    const content = await scrapeUrl(url);
    if (content === undefined) {
        res.status(500).send({error: "something went wrong trying to start "});
        
    } else {
        console.log("Got some content, outputting html");
        res.setHeader("Content-Type", "text/html");
        res.send(content);

    }
});

app.listen(8060, function () {
    console.log('Running on port 8060.');
});


