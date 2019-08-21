/**
 * @license
 * Copyright 2018 Google LLC. All Rights Reserved.
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * =============================================================================
 */

// Demo of TensorFlow.js mobilenet image classifier
// Usage:
//     node TFImageDemo.js <JpgImageFile>
// Input image file needs to be a .jpg file with size 224 x 224 pixels
// Top 3 predictions returned with probabilities
// based off TensorFlow.js' mobilenet example, https://github.com/tensorflow/tfjs-examples/tree/master/mobilenet
//   and this blog post, https://dev.to/ibmdeveloper/machine-learning-in-nodejs-with-tensorflowjs-1g1p

const tf = require('@tensorflow/tfjs');

global.fetch = require('node-fetch');

const fs = require('fs');
const jpeg = require('jpeg-js');

const NUMBER_OF_CHANNELS = 3

const IMAGENET_CLASSES = require('./imagenet_classes');


const MOBILENET_MODEL_PATH =
    // tslint:disable-next-line:max-line-length
    'https://storage.googleapis.com/tfjs-models/tfjs/mobilenet_v1_0.25_224/model.json';

const IMAGE_SIZE = 224;
const TOPK_PREDICTIONS = 3;

let mobilenet;
const mobilenetDemo = async (file) => {
    console.log('Loading model...');

    mobilenet = await tf.loadLayersModel(MOBILENET_MODEL_PATH);

    // Warmup the model. This isn't necessary, but makes the first prediction
    // faster. Call `dispose` to release the WebGL memory allocated for the return
    // value of `predict`.
    mobilenet.predict(tf.zeros([1, IMAGE_SIZE, IMAGE_SIZE, 3])).dispose();

    console.log('Done warming model');

    const logits = tf.tidy(() => {
        const image = readImage(file)
        const input = imageToInput(image, NUMBER_OF_CHANNELS)

        return mobilenet.predict(input)
    });

    // Convert logits to probabilities and class names.
    const classes = await getTopKClasses(logits, TOPK_PREDICTIONS);

    console.log('\nTop 3 probabilities:');
    console.log(classes);
}

const readImage = path => {
    const buf = fs.readFileSync(path)
    const pixels = jpeg.decode(buf, true)

    return pixels
}

const imageByteArray = (image, numChannels) => {
    const pixels = image.data
    const numPixels = image.width * image.height;
    const values = new Int32Array(numPixels * numChannels);

    // remove the alpha channel
    for (let i = 0; i < numPixels; i++) {
        for (let channel = 0; channel < numChannels; ++channel) {
            values[i * numChannels + channel] = pixels[i * 4 + channel];
        }
    }

    return values
}

const imageToInput = (image, numChannels) => {
    //const imgData = new ImageData(image.data, image.width, image.height);

    const values = imageByteArray(image, numChannels)
    const outShape = [image.height, image.width, numChannels];
    const img = tf.tensor3d(values, outShape, 'int32');

    // tf.browser.fromPixels() returns a Tensor from an image element.
    //const img = tf.browser.fromPixels(input).toFloat();

    const offset = tf.scalar(127.5);
    // Normalize the image from [0, 255] to [-1, 1].
    const normalized = img.sub(offset).div(offset);

    // Reshape to a single-element batch so we can pass it to predict.
    const batched = normalized.reshape([1, IMAGE_SIZE, IMAGE_SIZE, 3]);
    //const batched = normalized.reshape([1, image.height, image.width, 3]);

    return batched
}

/**
 * Computes the probabilities of the topK classes given logits by computing
 * softmax to get probabilities and then sorting the probabilities.
 * @param logits Tensor representing the logits from MobileNet.
 * @param topK The number of top predictions to show.
 */
async function getTopKClasses(logits, topK) {
    const values = await logits.data();

    const valuesAndIndices = [];
    for (let i = 0; i < values.length; i++) {
        valuesAndIndices.push({ value: values[i], index: i });
    }
    valuesAndIndices.sort((a, b) => {
        return b.value - a.value;
    });
    const topkValues = new Float32Array(topK);
    const topkIndices = new Int32Array(topK);
    for (let i = 0; i < topK; i++) {
        topkValues[i] = valuesAndIndices[i].value;
        topkIndices[i] = valuesAndIndices[i].index;
    }

    const topClassesAndProbs = [];
    for (let i = 0; i < topkIndices.length; i++) {
        topClassesAndProbs.push({
            className: IMAGENET_CLASSES[topkIndices[i]],
            probability: topkValues[i]
        })
    }
    return topClassesAndProbs;
}

if (process.argv.length !== 3) {
    throw new Error('incorrect number of arguments:\n \
        node TFImageDemo.js <JpgImageFile> \n');
}

mobilenetDemo(process.argv[2]);
