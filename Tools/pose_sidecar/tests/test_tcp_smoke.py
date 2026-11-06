"""Exercise the real framed TCP boundary without Unity, ML or webcam."""
import socket
import threading
import unittest

from Tools.pose_sidecar.main import make_header
from Tools.pose_sidecar.demo import synthetic_landmarks
from Tools.pose_sidecar.protocol import decode_packet, encode_packet


class TcpSmokeTest(unittest.TestCase):
    def test_packet_over_loopback(self):
        payload = encode_packet(make_header(
            42, 999, 1280, 720, synthetic_landmarks(0.5), [], demo=True))

        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server:
            server.bind(("127.0.0.1", 0))
            server.listen(1)
            port = server.getsockname()[1]
            failures = []

            def producer():
                try:
                    with server.accept()[0] as connected:
                        # Deliberately fragment the packet to test exact reads.
                        for offset in range(0, len(payload), 17):
                            connected.sendall(payload[offset:offset + 17])
                except OSError as error:
                    failures.append(error)

            thread = threading.Thread(target=producer, daemon=True)
            thread.start()
            with socket.create_connection(("127.0.0.1", port), timeout=3) as client:
                client.settimeout(3)
                with client.makefile("rb") as stream:
                    header, jpeg = decode_packet(stream)
            thread.join(timeout=3)

            self.assertFalse(failures)
            self.assertFalse(thread.is_alive(), "producer did not exit")
            self.assertEqual(header["frameId"], 42)
            self.assertEqual(len(header["landmarks"]), 33)
            self.assertEqual(jpeg, b"")


if __name__ == "__main__":
    unittest.main()
