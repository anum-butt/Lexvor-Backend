/// <binding Clean='clean:css' />
/// <binding  Build='build' />
var gulp = require('gulp'),
    autoprefixer = require('gulp-autoprefixer'),
    csscomb = require('gulp-csscomb'),
    cleanCss = require('gulp-clean-css'),
    concat = require('gulp-concat'),
    del = require('del'),
    postcss = require('gulp-postcss'),
    plumber = require('gulp-plumber'),
    sass = require('gulp-sass'),
    sourcemaps = require('gulp-sourcemaps'),
    uglify = require('gulp-uglify'),
    rename = require('gulp-rename'),
    wait = require('gulp-wait');


var paths = {
	base: {
		node: 'node_modules'
	},
	dist: {
		base: './wwwroot',
		css: './wwwroot/css',
		js: './wwwroot/js'
	},
	src: {
		base: './wwwroot',
		html: '**/*.html',
		css: 'css',
		js: 'js',
		img: 'img/**/*.+(png|jpg|gif|svg)',
		vendor: 'libs',
		resources: 'wwwroot/sources'
	}
}

// Clean CSS
gulp.task('clean:css', function (done) {
	return del([
		paths.dist.css + '/**/*.css'
	]);
});

// Compile SCSS

gulp.task('compile:scss', function (done) {
	return gulp
		.src(paths.src.resources + '/scss/**/*.scss')
		.pipe(wait(500))
		.pipe(sass().on('error', sass.logError))
		.pipe(postcss([require('postcss-flexbugs-fixes')]))
		.pipe(autoprefixer({
			browsers: ['> 1%']
		}))
		.pipe(csscomb())
		.pipe(gulp.dest(paths.dist.css));
});

// Minify CSS

gulp.task('minify:css', function (done) {
	return gulp.src(paths.dist.css + '/purpose.css')
		.pipe(sourcemaps.init())
		.pipe(cleanCss())
		.pipe(rename({
			suffix: '.min'
		}))
		.pipe(gulp.dest(paths.dist.css));
	done();
});

// Clean JS

gulp.task('clean:js', function (done) {
	return del([
		paths.dist.js + '/**/*.js'
	]);
	done();
});

// Concat JS

gulp.task('concat:js-core', function (done) {
	return gulp
		.src([
			'wwwroot/libs/jquery/dist/jquery.min.js',
			'wwwroot/libs/bootstrap/dist/js/bootstrap.bundle.min.js',
			'wwwroot/libs/in-view/dist/in-view.min.js',
			'wwwroot/libs/sticky-kit/dist/sticky-kit.min.js',
			'wwwroot/libs/svg-injector/dist/svg-injector.min.js',
			'wwwroot/libs/jquery.scrollbar/jquery.scrollbar.min.js',
			'wwwroot/libs/jquery-scroll-lock/dist/jquery-scrollLock.min.js',
			'wwwroot/libs/imagesloaded/imagesloaded.pkgd.min.js'
		])
		.pipe(concat('purpose.core.js'))
		.pipe(gulp.dest(paths.dist.js));

	done();
});

gulp.task('concat:vue', function (done) {
	return gulp
		.src([
			'wwwroot/libs/vue-base.js',
			'wwwroot/libs/axios.js',
			'wwwroot/libs/vue-the-mask.js',
		])
		.pipe(concat('vue-addons.js'))
		.pipe(gulp.dest(paths.dist.js));

	done();
});

gulp.task('concat:js', function (done) {
	return gulp
		.src([
			paths.src.resources + '/js/purpose/layout.js',
			paths.src.resources + '/js/purpose/init/*.js',
			paths.src.resources + '/js/purpose/custom/*.js',
			paths.src.resources + '/js/purpose/maps/*.js',
			paths.src.resources + '/js/purpose/charts/*.js',
			paths.src.resources + '/js/purpose/libs/*.js',
			paths.src.resources + '/js/purpose/charts/**/*js'
		])
		.pipe(concat('purpose.js'))
		.pipe(gulp.dest(paths.dist.js));
	done();
});

// Minify js
gulp.task('minify:js', function (done) {
	return gulp.src(paths.dist.js + '/purpose.js')
		.pipe(plumber())
		.pipe(sourcemaps.init())
		.pipe(uglify())
		.pipe(rename({
			suffix: '.min'
		}))
		.pipe(gulp.dest(paths.dist.js));
	done();
});
gulp.task('minify:vue', function (done) {
	return gulp.src(paths.dist.js + '/vue-addons.js')
		.pipe(plumber())
		.pipe(sourcemaps.init())
		.pipe(uglify())
		.pipe(rename({
			suffix: '.min'
		}))
		.pipe(gulp.dest(paths.dist.js));
	done();
});

// Copy CSS

gulp.task('copy:css', function (done) {
	return gulp.src([
			paths.src.base + '/assets/css/theme.css'
		])
		.pipe(gulp.dest(paths.dist.base + '/css'));
	done();
});

// Copy JS

gulp.task('copy:js', function (done) {
	return gulp.src([
			paths.src.base + '/sources/js/vue.js',
			paths.src.base + '/sources/js/vue.min.js'
		])
		.pipe(gulp.dest(paths.dist.base + '/js'));
	done();
});


// Bundled tasks

gulp.task('js', gulp.series('clean:js', 'concat:js-core', 'concat:vue', 'concat:js', 'minify:js', 'minify:vue', 'copy:js'));
gulp.task('css', gulp.series('clean:css', 'compile:scss', 'minify:css'));

// Build

gulp.task('build', gulp.series('css', 'js'));

// Default

gulp.task('default', gulp.series('compile:scss'));