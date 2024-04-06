# Speculatores

A program that collects and distributes information from public sources. Named for the recon element of ancient roman legions.

## Requirements

The following is required to run Speculatores
* a .NET environment (tested with mono)
* a postgres database

## Inputs

The following inputs are supported:
* RSS feeds

## Output

The following outputs are supported:
* E-Mail
* Mastodon

## Filters

The following filters are supported:
* Regular expressions

## Example config

```
<Config>
	<ProcessTime>1m</ProcessTime>
	<ConsoleLogLevel>Info</ConsoleLogLevel>
	<DatabaseConnectionString>User ID=databaseusername;Password=notsecure;Host=localhost;Port=5432;Database=database;</DatabaseConnectionString>
	<Input>
		<Rss>
			<Name>myrss</Name>
			<Url>https://exmaple.tld/rssfeed</Url>
			<Format>{title} - {description} - {link}</Format>
			<MaxAge>30d</MaxAge>
		</Rss>
	</Input>
	<Output>
		<Mastodon>
			<Name>mymasto</Name>
			<Url>https://mastodon.example.tld</Url>
			<AccessToken>xxxxxxxxxxxxxxxxxxx</AccessToken>
		</Mastodon>
		<Mail>
			<Name>mymail</Name>
			<ToName>Info</ToName>
			<ToAddress>info@exmaple.tld</ToAddress>
			<Subject>Important stuff</Subject>
		</Mail>
	</Output>
	<Filter>
		<Regex>
			<InputName>myrss</InputName>
			<OutputName>mymasto</OutputName>
			<OutputName>mymail</OutputName>
			<Pattern>.*</Pattern>
		</Regex>
	</Filter>
	<Mailer>
		<ServerAddress>mail.example.tld</ServerAddress>
		<ServerPort>587</ServerPort>
		<Username>user@exmpale.tld</Username>
		<Password>notsecure</Password>
		<SenderName>Test</SenderName>
		<SenderAddress>user@example.tld</SenderAddress>
		<AdminName>John Admin</AdminName>
		<AdminAddress>admin@example.tld</AdminAddress>
		<RepeatTime>3h</RepeatTime>
	</Mailer>
</Config>
```
