import fsWin from 'fswin';

const isWindows = process.platform.startsWith("win");

const hide = (path) => {
    if (isWindows) {
        var attributes = {
            IS_HIDDEN: true, //false means no
        };
        fsWin.setAttributesSync(path, attributes)
    }
}

// set folder attribute for 'node_modules' to 'hidden' on Windows to exclude folder content being indexed as assets in Unity.
hide("node_modules");