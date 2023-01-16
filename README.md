
#My WebServer

example to navigate webserer
-	url = <domainName>/index.html
	- path = <exeCurrentDirect>/index.html
	- if url ends in just "/" implicetly means index.html
	- if no exstention is added it is seen as a file will return 404 error
	
## supported files for transport
- html
- css
- javascript (not tested but should be implimented)
- png
- jpg
- jpeg (not tested but should be implimented)

## need to add
- server side functions (will use delegates)
	- pass both body data and just url data to delegates
	- extension for server sind function - .func