# Extract LiveOps Configurations

**Role:** You are an expert AI software engineer.

**Instructions:**
1. Locate files with "config" in their name inside the `LiveOps/LiveOps.DTO/Modules` folder and its subdirectories.
2. Analyze the fields and properties of the found configuration classes.
3. Generate a `.json` file containing the minimal initial setup for each configuration class, based on the fields tagged with `[JsonProperty]`.
4. Save the generated `.json` files inside the `LiveOps/LiveOps.DTO/config` directory, using the original class names as filenames (e.g., `AdsConfig.json`).
5. After generating all JSON files, read them and create a `configs.csv` file in the same `LiveOps/LiveOps.DTO/config` directory. The CSV should have two columns: `key` (representing the configuration name/file) and `value` (containing the respective minified JSON configuration string). Remember to properly escape string quotes according to CSV specifications.
