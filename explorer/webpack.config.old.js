// Template for webpack.config.js in Fable projects
// Find latest version in https://github.com/fable-compiler/webpack-config-template

// In most cases, you'll only need to edit the CONFIG object (after dependencies)
// See below if you need better fine-tuning of Webpack options

// Dependencies. Also required: core-js, fable-loader, fable-compiler, @babel/core,
// @babel/preset-env, babel-loader, sass, sass-loader, css-loader, style-loader, file-loader, resolve-url-loader
var path = require('path');
var webpack = require('webpack');
var HtmlWebpackPlugin = require('html-webpack-plugin');
var CopyWebpackPlugin = require('copy-webpack-plugin');
var MiniCssExtractPlugin = require('mini-css-extract-plugin');

var CONFIG = {
    // The tags to include the generated JS and CSS will be automatically injected in the HTML template
    // See https://github.com/jantimon/html-webpack-plugin
    indexHtmlTemplate: './public/index.html',
    fsharpEntry: './src/explorer.fsproj',
    cssEntry: './public/index.css',
    outputDir: './deploy',
    assetsDir: './ ',
    publicPath: '/', // Where the bundled files are accessible relative to server root
    devServerPort: 8080,
    // When using webpack-dev-server, you may need to redirect some calls
    // to a external API server. See https://webpack.js.org/configuration/dev-server/#devserver-proxy
    devServerProxy: undefined,
    // Use babel-preset-env to generate JS compatible with most-used browsers.
    // More info at https://babeljs.io/docs/en/next/babel-preset-env.html
    babel: {
        presets: [
            ['@babel/preset-env', {
                modules: "auto",
                // This adds polyfills when needed. Requires core-js dependency.
                // See https://babeljs.io/docs/en/babel-preset-env#usebuiltins
                // Note that you still need to add custom polyfills if necessary (e.g. whatwg-fetch)
                useBuiltIns: 'entry',
                corejs: 3
            }]
        ],
    }
}

// The HtmlWebpackPlugin allows us to use a template for the index.html page
// and automatically injects <script> or <link> tags for generated bundles.
var commonPlugins = [
    new HtmlWebpackPlugin({
        filename: 'index.html',
        template: resolve(CONFIG.indexHtmlTemplate)
    })
];

module.exports = (env, argv) => {
    var isProduction = argv.mode === 'production';
    var outputWebpackStatsAsJson = hasArg('--json');

    console.log("Bundling CLIENT for " + (isProduction ? "production" : "development") + "...");
    
    return {
        // In development, split the JavaScript and CSS files in order to
        // have a faster HMR support. In production bundle styles together
        // with the code because the MiniCssExtractPlugin will extract the
        // CSS in a separate files.
        entry: isProduction ? {
            app: [resolve(CONFIG.fsharpEntry), resolve(CONFIG.cssEntry)]
        } : {
            app: [resolve(CONFIG.fsharpEntry)],
            style: [resolve(CONFIG.cssEntry)]
        },
        // Add a hash to the output file name in production
        // to prevent browser caching if code changes
        output: {
            publicPath: CONFIG.publicPath,        
            path: resolve(CONFIG.outputDir),
            filename: isProduction ? '[name].[contenthash].js' : '[name].js',
        },
        mode: isProduction ? 'production' : 'development',
        devtool: isProduction ? 'source-map' : 'eval-source-map',
        optimization: {
            splitChunks: {
                chunks: 'all'
            },
            mangleExports: false
        },
        // Besides the HtmlPlugin, we use the following plugins:
        // PRODUCTION
        //      - MiniCssExtractPlugin: Extracts CSS from bundle to a different file
        //          To minify CSS, see https://github.com/webpack-contrib/mini-css-extract-plugin#minimizing-for-production    
        //      - CopyWebpackPlugin: Copies static assets to output directory
        // DEVELOPMENT
        //      - HotModuleReplacementPlugin: Enables hot reloading when code changes without refreshing
        plugins: isProduction ?
            commonPlugins.concat([
                new MiniCssExtractPlugin({ filename: 'style.[contenthash].css' }),
                new CopyWebpackPlugin({
                    patterns: [{
                        from: resolve(CONFIG.assetsDir) }]
                    })
            ])
            : commonPlugins.concat([
                new webpack.HotModuleReplacementPlugin(),
            ]),
        resolve: {
            // See https://github.com/fable-compiler/Fable/issues/1490
            symlinks: false
        },
        // Configuration for webpack-dev-server
        devServer: {
            // Necessary when using non-hash client-side routing
            // This assumes the index.html is accessible from server root
            // For more info, see https://webpack.js.org/configuration/dev-server/#devserverhistoryapifallback
            historyApiFallback: {
                index: '/'
            },
            publicPath: CONFIG.publicPath,
            contentBase: resolve(CONFIG.assetsDir),
            host: '0.0.0.0',
            port: CONFIG.devServerPort,
            proxy: CONFIG.devServerProxy,
            hot: true,
            inline: true
        },
        // - fable-loader: transforms F# into JS
        // - babel-loader: transforms JS to old syntax (compatible with old browsers)
        // - sass-loaders: transforms SASS/SCSS into JS
        // - file-loader: Moves files referenced in the code (fonts, images) into output folder
        module: {
            rules: [
                {
                    test: /\.fs(x|proj)?$/,
                    use: {
                        loader: 'fable-loader',
                        options: {
                            babel: CONFIG.babel,
                            silent: outputWebpackStatsAsJson,
                        }
                    }
                },
                {
                    test: /\.js$/,
                    exclude: /node_modules/,
                    use: {
                        loader: 'babel-loader',
                        options: CONFIG.babel
                    },
                },
                {
                    test: /\.(sass|scss|css)$/,
                    use: [
                        isProduction
                            ? MiniCssExtractPlugin.loader
                            : 'style-loader',
                        'css-loader',
                        'resolve-url-loader',
                        {
                            loader: 'sass-loader',
                            options: { implementation: require('sass') }
                        }
                    ],
                },
                {
                    test: /\.(png|jpg|jpeg|gif|svg|woff|woff2|ttf|eot)(\?.*)?$/,
                    use: ['file-loader']
                }
            ]
        }
    };
};

function resolve(filePath) {
    return path.isAbsolute(filePath) ? filePath : path.join(__dirname, filePath);
}

function hasArg(arg) {
    return arg instanceof RegExp
        ? process.argv.some(x => arg.test(x))
        : process.argv.indexOf(arg) !== -1;
}
