Groin Punch Simple Server

Run the server using

mvn package && java -jar target/SimpleServer-0.0.1.jar

To run the testing client run

mvn package && java -cp target/SimpleServer-0.0.1.jar Client

To listen for local data traffic use (as root)

tcpdump port 9999 -i lo -XX -s 8192

to communicate with the server use

nc -u localhost 9999

<PRE>-
Protocol:

[LOGIN command]
bits: value:       explanation:
32    0x00000000   secret
*     *            name - utf8
8     0x00         null termination of string
32    *            x coordinate
32    *            y coordinate
16    *            dx vector
16    *            dy vector
8     0-100        health            


[LOGIN response]
bits: value:       explanation:
8     0x75 (u)     response type
32    *            your secret key for further communication
16    *            your public id

[PLAYER response]
bits: value:       explanation:
8     0x70 (p)     response type
16    *            public id of the player
32    *            x coordinate
32    *            y coordinate
16    *            dx vector
16    *            dy vector
8     0-100        health            

A normal communication would be
client sends: \x00000000Tommy\x0..........................
server responds with LOGIN response and several PLAYER responses, such
as:

u........\x0002 - login response
p\x0000.......................... - player 1 response
p\x0001.......................... - player 2 response
</PRE>