<h3 align="center">Discord Message Deleter</h3>

  <p align="center">
    Just a program that deletes your messages in Discord.
    <br />
    <br />
    <a href="https://github.com/SnakePin/Discord-Message-Deleter/issues">Report Bug</a>
    ·
    <a href="https://github.com/SnakePin/Discord-Message-Deleter/issues">Request Feature</a>
    ·
    <a href="https://github.com/SnakePin/Discord-Message-Deleter/releases">Latest Release</a>
  </p>
</div>


<!-- ABOUT THE PROJECT -->
## About The Project

This program will delete your Discord messages in the channels or guilds that you want it to work on. It also has an option to delete messages from all DMs or all guilds.


<!-- GETTING STARTED -->
### Prerequisites

You'll need the following to be able to run this program:
* .NET Framework 4.8


<!-- USAGE  -->
## Usage

All you have to do is download the <a href="https://github.com/SnakePin/Discord-Message-Deleter/releases">latest release</a> of the program, unzip the zip file somewhere and run the exe file.

You'll get an interface with a text box. Put the IDs of the channels where you want to delete your messages from.

For Guild IDs you'll have to prefix them with a "G" and for user IDs "U".

As for the Auth ID, you can get it by doing the following steps:
1. Log into Discord from a web browser
2. Open the DevTools(F12) go to the "Network" tab.
3. Select the "Fetch/XHR" filter.
4. Go to a random channel, then select the latest request that has appeared in the DevTools.
5. Scroll down in the "Headers" tab and find the header named "Authorization".
6. Select everything after the "Authorization: ", that is your Auth ID.


<!-- CONTRIBUTING -->
## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/FeatureName`)
3. Commit your Changes (`git commit -m 'Add some FeatureName'`)
4. Push to the Branch (`git push origin feature/FeatureName`)
5. Open a Pull Request


<!-- LICENSE -->
## License

Distributed under the MIT License. See `LICENSE` for more information.
