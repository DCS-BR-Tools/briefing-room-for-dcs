  const simpleDownload = (file, filename) => {
  // // Create the <a> element and click on it
  const exportUrl = URL.createObjectURL(file);
  const a = document.createElement("a");
  document.body.appendChild(a);
  a.href = exportUrl;
  a.download = filename;
  a.target = "_self";
  a.click();
  // We don't need to keep the url, let's release the memory
  // On Safari it seems you need to comment this line... (please let me know if you know why)
  URL.revokeObjectURL(exportUrl);
}


async function BlazorDownloadFile(filename, contentType, data) {

  
  
  // Create the URL
  const fileType = filename.split(".").at(-1)
  const file = new File([data], filename, { type: contentType });
  if (window.self !== window.top) {
    window.top.postMessage({
      filename,
      contentType,
      file
    }, "*")
    return
  }
  if (self.showSaveFilePicker) {
    try {
      const fileHandle = await self.showSaveFilePicker({
        suggestedName: filename,
        types: [{
          description: 'Text documents',
          accept: {
            'text/plain': [`.${fileType}`],
          },
        }],
      });

      const writable = await fileHandle.createWritable();
      // Write the contents of the file to the stream.
      await writable.write(file);
      // Close the file and write the contents to disk.
      await writable.close();
    } catch (error) {
      if(error.name === "AbortError") {
        return
      }
      console.error(error)
      simpleDownload(file, filename)
      return
    }
  
  } else {
    simpleDownload(file, filename)
  }

}

const Memoize = (fn) => {
  let cache = {};
  return async (...args) => {
    let strX = JSON.stringify(args);
    return strX in cache
      ? cache[strX]
      : (cache[strX] = await fn(...args));
  };
};


