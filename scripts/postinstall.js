const winattr = require("winattr");

const isWindows = process.platform.startsWith("win");

const hide = (path) => {
    if (isWindows) {
        winattr.set(path,  {hidden:true}, () => {});
    }
}

// set folder attribute for 'node_modules' to 'hidden' on Windows to exclude folder content being indexed as assets in Unity.
hide("node_modules");