Groin Punch Simple Server

Run the server using

mvn package && java -jar target/SimpleServer-0.0.1.jar

To run the testing client run

mvn package && java -cp target/SimpleServer-0.0.1.jar Client

To listen for local data traffic use (as root)

tcpdump port 9999 -i lo -XX -s 8192

to communicate with the server use

nc -u localhost 9999
