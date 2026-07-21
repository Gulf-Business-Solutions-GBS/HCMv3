import gulp from 'gulp';
import path from 'path';
import fs from 'fs';
import orderedReadStreams from 'ordered-read-streams';
import { globbySync } from 'globby';
import concat from 'gulp-concat';
import less from 'gulp-less';
import gulpTerser from 'gulp-terser';
import { minify } from 'terser';
import cleanCss from 'gulp-clean-css';

const __dirname = import.meta.dirname;

const bundleConfig = JSON.parse(fs.readFileSync('./bundles.json', 'utf-8'));
var production = false;

var styleEntries = {};
var scriptEntries = {};

function processInputDefinition(input) {
    var result = [];
    for (var i = 0; i < input.length; i++) {
        var url = input[i];
        var longPath;
        if (url.startsWith('!')) {
            longPath = '!' + path.resolve(__dirname, url.substring(1));
        } else {
            longPath = path.resolve(__dirname, url);
        }

        longPath = longPath.replace(/\\/g, '/');
        result.push(longPath);
    }

    return result;
}

function fillScriptBundles() {
    // User defined bundles
    for (var k = 0; k < bundleConfig.scripts.length; k++) {
        var scriptBundle = bundleConfig.scripts[k];
        checkBundleItem(scriptBundle);
        scriptEntries[scriptBundle.output] = globbySync(scriptBundle.input);
    }
}

function fillStyleBundles() {
    // User defined styles
    for (var k = 0; k < bundleConfig.styles.length; k++) {
        var styleBundle = bundleConfig.styles[k];
        checkBundleItem(styleBundle);
        styleEntries[styleBundle.output] = globbySync(styleBundle.input);
    }
}

function getFileNameFromPath(path) {
    return path.substring(path.lastIndexOf('/') + 1);
}

function getPathWithoutFileNameFromPath(path) {
    return path.substring(0, path.lastIndexOf('/'));
}

function createScriptBundles() {
    var tasks = [];
    for (var script in scriptEntries) {
        tasks.push(createScriptBundle(script));
    }

    return tasks;
}

function createScriptBundle(script) {
    var bundleName = getFileNameFromPath(script);
    var bundlePath = getPathWithoutFileNameFromPath(script);

    var stream = gulp.src(scriptEntries[script]);

    if (production) {
        stream = stream.pipe(gulpTerser({}, minify));
    }

    return stream.pipe(concat(bundleName)).pipe(gulp.dest(bundlePath));
}

function createStyleBundles() {
    var tasks = [];
    for (var style in styleEntries) {
        tasks.push(createStyleBundle(style));
    }

    return tasks;
}

function createStyleBundle(style) {
    var bundleName = getFileNameFromPath(style);
    var bundlePath = getPathWithoutFileNameFromPath(style);

    var stream = gulp.src(styleEntries[style]).pipe(less({ math: 'parens-division' }));

    if (production) {
        stream = stream.pipe(cleanCss());
    }

    return stream.pipe(concat(bundleName)).pipe(gulp.dest(bundlePath));
}

function checkBundleItem(bundleItem) {
    var definition = processInputDefinition(bundleItem.input);

    for (var i = 0; i < definition.length; i++) {
        var url = definition[i];
        if (
            typeof url == 'undefined' ||
            url.startsWith('!') ||
            url.indexOf('*') >= 0 ||
            url.match(/^[0-9]+$/) != null //only digit
        ) {
            continue;
        }

        checkFile(url);
    }
}

function checkFile(path) {
    try {
        if (fs.existsSync(path)) {
            //file exists
        } else {
            console.error('File not found: ' + path);
        }
    } catch {
        console.error('File not found: ' + path);
    }
}

function build() {
    production = true;

    fillScriptBundles();
    fillStyleBundles();

    var scriptTasks = createScriptBundles();
    var styleTasks = createStyleBundles();

    return orderedReadStreams(scriptTasks.concat(styleTasks));
}

function buildDev() {
    fillScriptBundles();
    fillStyleBundles();

    var scriptTasks = createScriptBundles();
    var styleTasks = createStyleBundles();

    console.log('Dynamic bundles are being created.');

    return orderedReadStreams(scriptTasks.concat(styleTasks));
}

export { build as build, buildDev as buildDev };
