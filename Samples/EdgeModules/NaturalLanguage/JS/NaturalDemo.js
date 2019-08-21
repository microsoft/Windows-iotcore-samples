// Demo using https://github.com/NaturalNode/natural
// Leverage the sentiment component to analyze strings to see if the string's
// sentiment is positive or negative.
// Use BayesClassifier to build a model of the seen strings + sentiment rating.
// Model is saved and loaded from classifierFile to improve model over time.
// Predict sentiment based on the model.

const natural = require('natural');
const tokenizer = new natural.TreebankWordTokenizer();

const Analyzer = natural.SentimentAnalyzer;
const stemmer = natural.PorterStemmer;

// Use senticon for multilingual support
const analyzer = new Analyzer("English", stemmer, "senticon");

const classifierFile = "classifierData.json";

const classifyString = (inputString, classifier) => {
    console.log(inputString);

    const tokens = tokenizer.tokenize(inputString);

    // getSentiment expects an array of strings
    const sentimentValue = analyzer.getSentiment(tokens);

    var sentimentRating;
    if (sentimentValue < 0) {
        sentimentRating = "negative";
    } else {
        sentimentRating = "positive";
    }

    console.log("Sentiment value " + sentimentValue + ", rating " + sentimentRating);

    classifier.addDocument(inputString, sentimentRating);
    classifier.train();

    console.log("Classifer result: " + classifier.classify(inputString) + "\n");

}

const classifySet = (classifier) => {
 
    classifyString("This product is awesome.", classifier);
    classifyString("This product has problems.", classifier);
    classifyString("This product hits severe issues.", classifier);
    classifyString("This product crashes horribly.", classifier);
    classifyString("This product has great update support.", classifier);
    classifyString("This product has security fixes.", classifier);

    saveClassifier(classifier, classifierFile);
}

async function startClassifier (file) {
    const fs = require('fs');

    if (fs.existsSync(file)) {
        natural.BayesClassifier.load(file, null, function (err, classfr) {
            if (err) {
                throw err;
            }
            classifySet(classfr);
        });
    } else {
        var classifier = new natural.BayesClassifier();
        classifySet(classifier);
    }

    return;
}

const saveClassifier = (classfr, file) => {
    classfr.save(file, function (err, classifier) {
        if (err) {
            throw err;
        }
    });
}

async function DemoMain () {
    await startClassifier(classifierFile);
}

DemoMain();
