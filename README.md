<h1 align="center">Collection Sections</h1>
<h2 align="center">A Jellyfin Plugin</h2>
<p align="center">
	<img alt="Logo" src="https://raw.githubusercontent.com/IAmParadox27/jellyfin-plugin-collection-sections/main/src/logo.png" />
	<br />
	<br />
	<a href="https://github.com/IAmParadox27/jellyfin-plugin-home-sections">
		<img alt="GPL 3.0 License" src="https://img.shields.io/github/license/IAmParadox27/jellyfin-plugin-collection-sections.svg" />
	</a>
	<a href="https://github.com/IAmParadox27/jellyfin-plugin-collection-sections/releases">
		<img alt="Current Release" src="https://img.shields.io/github/release/IAmParadox27/jellyfin-plugin-collection-sections.svg" />
	</a>
</p>

## Development Update - 20th August 2025

Hey all! Things are changing with my plugins are more and more people start to use them and report issues. In order to make it easier for me to manage I'm splitting bugs and features into different areas. For feature requests please head over to <a href="https://features.iamparadox.dev/">https://features.iamparadox.dev/</a> where you'll be able to signin with GitHub and make a feature request. For bugs please report them on the relevant GitHub repo and they will be added to the <a href="https://github.com/users/IAmParadox27/projects/1/views/1">project board</a> when I've seen them. I've found myself struggling to know when issues are made and such recently so I'm also planning to create a system that will monitor a particular view for new issues that come up and send me a notification which should hopefully allow me to keep more up to date and act faster on various issues.

As with a lot of devs, I am very momentum based in my personal life coding and there are often times when these projects may appear dormant, I assure you now that I don't plan to let these projects go stale for a long time, there just might be times where there isn't an update or response for a couple weeks, but I'll try to keep that better than it has been. With all new releases to Jellyfin I will be updating as soon as possible, I have already made a start on 10.11.0 and will release an update to my plugins hopefully not long after that version is officially released!

## Introduction
Collection Sections is a Jellyfin plugin extension for [Home Screen Sections](https://www.github.com/IAmParadox27/jellyfin-plugin-home-sections) which allows users to add content from collections to the web client's home screen.

When coupled with an automatic collection plugin this can create very dynamic sections on your home screen. My uses for the plugin include having `Trending`, `Most Watched this Week` and `Award Winning` sections on my home screen.

## Installation

### Prerequisites
- This plugin is based on Jellyfin Version `10.10.7`
- The following plugins are required to also be installed, please following their installation guides:
  - Home Screen Sections (https://github.com/IAmParadox27/jellyfin-plugin-home-sections) at least v2.3.8.0

### Installation
1. Add `https://www.iamparadox.dev/jellyfin/plugins/manifest.json` to your plugin repositories.
2. Install the prerequisite plugins by following the Home Screen Sections install guide.
3. Install `Collection Sections` from the Catalogue.
4. Restart Jellyfin.
