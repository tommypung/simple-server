package com.groinpunchstudios.simple;

import java.io.IOException;

public class Main
{
	public static void main(String[] args) throws IOException, InterruptedException
	{
		int port = 9999;
		Server server = new Server(port);
		server.start();
		server.join();
	}
}
