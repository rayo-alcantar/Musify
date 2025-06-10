# Musify Desktop App (Intento4)

A desktop music player application built with WPF (.NET).

## Features

*   Browse and play songs from a central library.
*   User registration and login.
*   **Private Playlists:** Users can create their own playlists, which are private and only visible to them.
    *   Create new playlists.
    *   Add songs from the library to their playlists.
*   Song playback with basic controls (play, pause, next, previous).
*   Dynamic playlist generation based on categories (mood, activity, time of day).
*   Administrator dashboard for managing songs (details not covered by current modifications).

## Setup & Running

1.  **Prerequisites:**
    *   .NET SDK (version compatible with the project, likely .NET 8 or newer).
    *   Access to the MongoDB instance specified in the connection strings within the code (currently using a cloud MongoDB Atlas instance).
2.  **Running the Application:**
    *   Clone the repository.
    *   Open the solution file (`Intento4.sln`) in Visual Studio.
    *   Build the solution.
    *   Run the application (usually by starting the `Intento4` project).

## Project Structure

*   `Intento4/`: Main WPF project directory.
    *   `Models/`: Contains data models (e.g., `Usuario.cs`, `Playlist.cs`, `Cancion.cs`).
    *   `Servicios/`: Contains services like `MongoDBService.cs` for database interactions.
    *   `Views/Pages`: Contains XAML pages for different sections of the app (e.g., `BibliotecaPage.xaml`, `LoginWindow.xaml`).
    *   `Images/`: Contains images used in the application.
*   `.gitignore`: Specifies intentionally untracked files that Git should ignore.
*   `Intento4.sln`: Visual Studio solution file.

## Notes

*   The application connects to a MongoDB Atlas cluster. Ensure the connection string in `MongoDBService.cs` (and other places it might be hardcoded, e.g., `LoginWindow.xaml.cs`) is correctly configured if you are using your own database.
*   Password storage is currently plaintext (as per issue instructions to ignore this). For a production environment, password hashing should be implemented.
