import gulp from 'gulp';
import path from 'path';
import fs from 'fs';
import orderedReadStreams from 'ordered-read-streams';
import { globbySync } from 'globby';
import concat from 'gulp-concat';
import less from 'gulp-less';
import cleanCss from 'gulp-clean-css';
import postcss from 'gulp-postcss';
import url from 'postcss-url';

const __dirname = import.meta.dirname;

const bundleConfig = JSON.parse(fs.readFileSync('./bundles.json', 'utf-8'));

var production = false;

var styleEntries = {};

var viewStyles = globbySync([
  './wwwroot/view-resources/**/*.css',
  './wwwroot/view-resources/**/*.less',
  '!./wwwroot/view-resources/**/*.min.css',
]);

function processInputDefinition(input) {
  var result = [];
  for (var i = 0; i < input.length; i++) {
    var url = input[i];
    if (url.startsWith('!')) {
      result.push('!' + path.resolve(__dirname, url.substring(1)));
    } else {
      result.push(path.resolve(__dirname, url));
    }
  }

  return result;
}

function fillStyleBundles() {
  // User defined styles
  for (var k = 0; k < bundleConfig.styles.length; k++) {
    var styleBundle = bundleConfig.styles[k];
    checkBundleItem(styleBundle);
    styleEntries[styleBundle.output] = globbySync(styleBundle.input);
  }

  // View styles
  for (var j = 0; j < viewStyles.length; j++) {
    var viewStyleName = viewStyles[j].replace('./wwwroot/', '');

    if (viewStyleName.indexOf('.css') >= 0) {
      styleEntries[viewStyleName.replace('.css', '.min.css')] = [path.resolve(__dirname, 'wwwroot/' + viewStyleName)];
    }

    if (viewStyleName.indexOf('.less') >= 0) {
      styleEntries[viewStyleName.replace('.less', '.min.css')] = [path.resolve(__dirname, 'wwwroot/' + viewStyleName)];
    }
  }
}

function getFileNameFromPath(fullPath) {
  return path.basename(fullPath);
}

function getPathWithoutFileNameFromPath(fullPath) {
  return path.dirname(fullPath);
}

function createStyleBundles() {
  var tasks = [];
  for (var style in styleEntries) {
    tasks.push(createStyleBundle(style));
  }

  return tasks;
}

function createStyleBundle(style) {
  let basePath = process.argv.length >= 5 ? process.argv[4] : '';

  var bundleName = getFileNameFromPath(style);
  var bundlePath = getPathWithoutFileNameFromPath(style);

  var options = {
    url: function (asset) {
      // Ignore absolute URLs
      if (asset.url.substring(0, 1) === '/') {
        return asset.url;
      }

      var outputFolder = '';

      if (asset.url.match(/\.(png|svg|jpg|gif)$/)) {
        outputFolder = basePath + 'dist/img';
      } else if (asset.url.match(/\.(woff|woff2|eot|ttf|otf)[?]{0,1}.*$/)) {
        outputFolder = basePath + 'dist/fonts';
      } else {
        // Ignore not recognized assets like data:image etc...
        return asset.url;
      }

      var fileName = path.basename(asset.absolutePath);
      var outputPath = path.join(__dirname, '/wwwroot/' + outputFolder + '/');

      gulp.src(asset.absolutePath, { encoding: false }).pipe(gulp.dest(outputPath));

      return '/' + outputFolder + '/' + fileName;
    },
  };

  var stream = gulp
    .src(styleEntries[style], { encoding: false })
    .pipe(postcss([url(options)]))
    .pipe(less({ math: 'parens-division' }));

  if (production) {
    stream = stream.pipe(cleanCss());
  }

  return stream.pipe(concat(bundleName)).pipe(gulp.dest('wwwroot/' + bundlePath));
}

function findMatchingElements(path, array) {
  var result = [];
  for (var item in array) {
    if (array[item].indexOf(path) >= 0) {
      result[item] = array[item];
    }
  }

  return result;
}

function watchStyleEntries() {
  for (var style in styleEntries) {
    var watcher = gulp.watch(styleEntries[style]);

    watcher.on('change', function (path, stats) {
      console.log(`${path} updated`);

      var changedBundles = findMatchingElements(path, styleEntries);

      for (var changedBundle in changedBundles) {
        createStyleBundle(changedBundle);
      }
    });
  }
}

function build() {
  production = true;

  fillStyleBundles();

  var styleTasks = createStyleBundles();

  return orderedReadStreams(styleTasks);
}

function buildDev() {
  fillStyleBundles();

  var styleTasks = createStyleBundles();

  watchStyleEntries();

  console.log('Bundles are being created, please wait...');

  return orderedReadStreams(styleTasks);
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
  } catch (err) {
    console.error('File not found: ' + path);
  }
}

export { build as build, buildDev as buildDev };
